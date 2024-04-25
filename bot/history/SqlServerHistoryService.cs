using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

public class SqlServerHistoryService(IConfig config, ILogger<SqlServerHistoryService> logger)
: HistoryServiceBase, IHistoryService
{
    private readonly IConfig config = config;
    private readonly ILogger<SqlServerHistoryService> logger = logger;

    private bool IsTransientFault(Exception ex)
    {
        if (ex is null)
        {
            return false;
        }

        if (ex is SqlException sqlException)
        {
            // enumerate through all errors found in the exception.
            // NOTE: additional information on the codes, can be found here:
            //    https://github.com/Azure/elastic-db-tools/blob/master/Src/ElasticScale.Client/ElasticScale.Common/TransientFaultHandling/Implementation/SqlDatabaseTransientErrorDetectionStrategy.cs
            foreach (SqlError err in sqlException.Errors)
            {
                switch (err.Number)
                {
                    case 49920: // cannot process request. Too many operations in progress for subscription
                    case 49919: // cannot process create or update request. Too many create or update operations in progress for subscription
                    case 49918: // cannot process request. Not enough resources to process request
                    case 41839: // transaction exceeded the maximum number of commit dependencies and the last statement was aborted.
                    case 40501: // service is busy
                    case 10928:
                    case 10929:
                    case 10053:
                    case 10054:
                    case 10060:
                    case 40197:
                    case 40540:
                    case 40613:
                    case 40143:
                    case 4221: // login to read-secondary failed due to long wait on 'HADR_DATABASE_WAIT_FOR_TRANSITION_TO_VERSIONING'
                    case 233:
                    case 64:
                    case 20: // doesn't support encryption
                        return true;
                }
            }
        }
        else if (ex is TimeoutException)
        {
            return true;
        }

        return false;
    }

    private Task ExecuteWithRetryOnTransient(Func<Task> onExecuteAsync, Func<Exception, TimeSpan, Task> onRetryAsync)
    {
        return Policy
            .Handle<Exception>(this.IsTransientFault)
            .WaitAndRetryAsync(
                this.config.SQL_SERVER_MAX_RETRY_ATTEMPTS,
                retryAttempt => TimeSpan.FromSeconds(this.config.SQL_SERVER_SECONDS_BETWEEN_RETRIES),
                onRetryAsync: onRetryAsync)
            .ExecuteAsync(onExecuteAsync);
    }

    public async Task AddInteractionAsync(IInteraction interaction)
    {
        // TODO: provision tables
        base.ValidateAddInteractionAsync(interaction);
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to insert interaction for user {u} into the history database...", interaction.UserId);
                using var connection = new SqlConnection(config.SQL_SERVER_HISTORY_SERVICE_CONNSTRING);
                connection.Open();
                using var transaction = connection.BeginTransaction();
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    DECLARE @conversationId UNIQUEIDENTIFIER;
                    DECLARE @lastState VARCHAR(20);

                    SELECT TOP 1
                        @conversationId = [ConversationId],
                        @lastState = [State]
                    FROM [dbo].[History]
                    WHERE [UserId] = @userId
                    ORDER BY [Created] DESC;

                    IF @lastState = 'generating'
                        THROW 50100, 'already generating a response', 1

                    INSERT INTO [dbo].[History]
                        ([ConversationId], [ActivityId], [UserId], [Role], [Message], [State], [Expiry])
                    VALUES
                        (@conversationId, @activityId, @userId, @role, @message, @state, @expiry);
                ";
                command.Parameters.AddWithValue("@activityId", interaction.ActivityId);
                command.Parameters.AddWithValue("@userId", interaction.UserId);
                command.Parameters.AddWithValue("@role", interaction.Role.ToString().ToLower());
                command.Parameters.AddWithValue("@message", interaction.Message);
                command.Parameters.AddWithValue("@state", interaction.State.ToString().ToLower());
                command.Parameters.AddWithValue("@expiry", DateTime.UtcNow.AddDays(90));
                try
                {
                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
                this.logger.LogInformation("successfully inserted interaction for user {u} into the history database.", interaction.UserId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "inserting interaction for user {u} raised the following SQL transient exception...", interaction.UserId);
                return Task.CompletedTask;
            });
    }

    public Task<IConversation> ChangeConversationTopicAsync(string userId)
    {
        throw new System.NotImplementedException();
    }

    public Task ClearFeedbackAsync(string userId)
    {
        throw new System.NotImplementedException();
    }

    public Task ClearFeedbackAsync(string userId, string activityId)
    {
        throw new System.NotImplementedException();
    }

    public Task CommentOnMessageAsync(string userId, string comment)
    {
        throw new System.NotImplementedException();
    }

    public Task CommentOnMessageAsync(string userId, string activityId, string comment)
    {
        throw new System.NotImplementedException();
    }

    public Task DeleteLastInteractionsAsync(string userId, int count = 1)
    {
        throw new System.NotImplementedException();
    }

    public Task<IConversation> GetCurrentConversationAsync(string userId)
    {
        throw new System.NotImplementedException();
    }

    public Task RateMessageAsync(string userId, string rating)
    {
        throw new System.NotImplementedException();
    }

    public Task RateMessageAsync(string userId, string activityId, string rating)
    {
        throw new System.NotImplementedException();
    }

    /*
        string ConversationId { get; set; }
        string ActivityId { get; set; }
        string UserId { get; set; }
        Roles Role { get; set; }
        string Message { get; set; }
        States State { get; set; }
        string Rating { get; set; }
        string Comment { get; set; }
        DateTime Created { get; set; }
        DateTime Expiry { get; set; }
        int PromptTokenCount { get; set; }
        int CompletionTokenCount { get; set; }
        int TimeToFirstResponse { get; set; }
        int TimeToLastResponse { get; set; }
    */


    public async Task Startup()
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to verify or create the History table...");
                using var connection = new SqlConnection(config.SQL_SERVER_HISTORY_SERVICE_CONNSTRING);
                connection.Open();
                using var transaction = connection.BeginTransaction();
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'dbo.History')
                    BEGIN
                        CREATE TABLE [dbo].[History]
                        (
                            [ConversationId] UNIQUEIDENTIFIER NOT NULL,
                            [ActivityId] UNIQUEIDENTIFIER NOT NULL,
                            [UserId] UNIQUEIDENTIFIER NOT NULL,
                            [Role] VARCHAR(20) NOT NULL,
                            [Message] NVARCHAR(MAX) NULL,
                            [State] NVARCHAR(20) NOT NULL,
                            [Rating] VARCHAR(10) NULL,
                            [Comment] NVARCHAR(MAX) NULL,
                            [Created] DATETIME DEFAULT GETDATE(),
                            [Expiry] DATETIME NOT NULL,
                            [PromptTokenCount] INT,
                            [CompletionTokenCount] INT,
                            [TimeToFirstResponse] INT,
                            [TimeToLastResponse] INT
                        )
                    END";
                try
                {
                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
                this.logger.LogInformation("successfully verified or created the History table.");
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "verifying or creating the History table raised the following SQL transient exception...");
                return Task.CompletedTask;
            });
    }
}