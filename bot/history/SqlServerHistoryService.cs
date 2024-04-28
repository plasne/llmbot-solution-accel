using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetBricks;
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

    public async Task StartGenerationAsync(Interaction request, Interaction response)
    {
        base.ValidateInteractionForStartGeneration(request);
        base.ValidateInteractionForStartGeneration(response);
        try
        {
            await this.ExecuteWithRetryOnTransient(
                async () =>
                {
                    this.logger.LogDebug("attempting to insert interaction for user {u} into the history database...", request.UserId);
                    using var connection = new SqlConnection(config.SQL_SERVER_HISTORY_SERVICE_CONNSTRING);
                    await connection.OpenAsync();
                    using var transaction = connection.BeginTransaction(); // rollback is automatic during dispose
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = @"
                        DECLARE @conversationId UNIQUEIDENTIFIER;
                        DECLARE @lastState VARCHAR(20);

                        SELECT TOP 1
                            @conversationId = [ConversationId],
                            @lastState = [State]
                        FROM [dbo].[History]
                        WHERE [UserId] = @req_userId
                        ORDER BY [Id] DESC;

                        SET @conversationId = ISNULL(@conversationId, NEWID());

                        IF @lastState = 'GENERATING'
                            THROW 50100, 'already generating a response', 1

                        INSERT INTO [dbo].[History]
                            ([ConversationId], [ActivityId], [UserId], [Role], [Message], [State], [Expiry])
                        VALUES
                            (@conversationId, @req_activityId, @req_userId, @req_role, @req_message, @req_state, @expiry),
                            (@conversationId, @res_activityId, @res_userId, @res_role, NULL, @res_state, @expiry);
                    ";
                    command.Parameters.AddWithValue("@req_activityId", request.ActivityId);
                    command.Parameters.AddWithValue("@res_activityId", response.ActivityId);
                    command.Parameters.AddWithValue("@req_userId", request.UserId);
                    command.Parameters.AddWithValue("@res_userId", response.UserId);
                    command.Parameters.AddWithValue("@req_role", request.Role.ToString().ToUpper());
                    command.Parameters.AddWithValue("@res_role", response.Role.ToString().ToUpper());
                    command.Parameters.AddWithValue("@req_message", request.Message);
                    command.Parameters.AddWithValue("@req_state", request.State.ToString().ToUpper());
                    command.Parameters.AddWithValue("@res_state", response.State.ToString().ToUpper());
                    command.Parameters.AddWithValue("@expiry", DateTime.UtcNow.AddDays(90));
                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                    this.logger.LogInformation("successfully inserted interaction for user {u} into the history database.", request.UserId);
                }, (ex, _) =>
                {
                    this.logger.LogError(ex, "inserting interaction for user {u} raised the following SQL transient exception...", request.UserId);
                    return Task.CompletedTask;
                });
        }
        catch (SqlException ex)
        {
            if (ex.Number == 50100)
            {
                throw new AlreadyGeneratingException(request.UserId!);
            }
            throw;
        }
    }

    public async Task CompleteGenerationAsync(Interaction response)
    {
        base.ValidateInteractionForCompleteGeneration(response);
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to complete interaction for user {u} into the history database...", response.UserId);
                using var connection = new SqlConnection(config.SQL_SERVER_HISTORY_SERVICE_CONNSTRING);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = connection.BeginTransaction(); // rollback is automatic during dispose
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE [dbo].[History]
                    SET [Message] = @message, [State] = @state, [Intent] = @intent,
                        [PromptTokenCount] = @promptTokenCount, [CompletionTokenCount] = @completionTokenCount,
                        [TimeToFirstResponse] = @timeToFirstResponse, [TimeToLastResponse] = @timeToLastResponse
                    WHERE [UserId] = @userId AND [ActivityId] = @activityId;
                ";
                command.Parameters.AddWithValue("@userId", response.UserId);
                command.Parameters.AddWithValue("@activityId", response.ActivityId);
                command.Parameters.AddWithValue("@message", response.Message);
                command.Parameters.AddWithValue("@state", response.State.ToString().ToUpper());
                command.Parameters.AddWithValue("@intent", response.Intent.ToString().ToUpper());
                command.Parameters.AddWithValue("@promptTokenCount", response.PromptTokenCount);
                command.Parameters.AddWithValue("@completionTokenCount", response.CompletionTokenCount);
                command.Parameters.AddWithValue("@timeToFirstResponse", response.TimeToFirstResponse);
                command.Parameters.AddWithValue("@timeToLastResponse", response.TimeToLastResponse);
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                this.logger.LogInformation("successfully completed interaction for user {u} into the history database.", response.UserId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "completing interaction for user {u} raised the following SQL transient exception...", response.UserId);
                return Task.CompletedTask;
            });
    }

    public Task<Conversation> ChangeConversationTopicAsync(string userId)
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

    public async Task<Conversation> GetCurrentConversationAsync(string userId)
    {
        var conversation = new Conversation { Interactions = [] };
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to get the current conversation for user {u} from the history database...", userId);
                using var connection = new SqlConnection(config.SQL_SERVER_HISTORY_SERVICE_CONNSTRING);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT [ConversationId], [ActivityId], [UserId], [Role], [Message], [State]
                    FROM [dbo].[History]
                    WHERE [ConversationId] IN (
                        SELECT TOP 1 [ConversationId]
                        FROM [dbo].[History]
                        WHERE [UserId] = @userId
                        ORDER BY [Id] DESC)
                    AND [State] IN ('EDITED', 'STOPPED', 'UNMODIFIED')
                    ORDER BY [Id] ASC
                ";
                command.Parameters.AddWithValue("@userId", userId);
                using var reader = await command.ExecuteReaderAsync();
                var conversationIdOrdinal = reader.GetOrdinal("ConversationId");
                var activityIdOrdinal = reader.GetOrdinal("ActivityId");
                var userIdOrdinal = reader.GetOrdinal("UserId");
                var roleOrdinal = reader.GetOrdinal("Role");
                var messageOrdinal = reader.GetOrdinal("Message");
                var stateOrdinal = reader.GetOrdinal("State");
                while (await reader.ReadAsync())
                {
                    var interaction = new Interaction
                    {
                        ConversationId = reader.GetGuid(conversationIdOrdinal),
                        ActivityId = reader.GetString(activityIdOrdinal),
                        UserId = reader.GetString(userIdOrdinal),
                        Role = reader.GetString(roleOrdinal).AsEnum(() => Roles.UNKNOWN),
                        Message = reader.GetString(messageOrdinal),
                        State = reader.GetString(stateOrdinal).AsEnum(() => States.UNKNOWN),
                    };
                    conversation.Interactions.Add(interaction);
                }
                conversation.Id = conversation.Interactions.FirstOrDefault()?.ConversationId;
                this.logger.LogInformation(
                    "successfully obtained current conversation for user {u} from the history database containing {n} turns.",
                    userId,
                    conversation.Interactions.Count);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "getting the current conversation for user {u} raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
        return conversation;
    }

    public Task RateMessageAsync(string userId, string rating)
    {
        throw new System.NotImplementedException();
    }

    public Task RateMessageAsync(string userId, string activityId, string rating)
    {
        throw new System.NotImplementedException();
    }

    public async Task StartupAsync()
    {
        this.logger.LogInformation("starting up SqlServerHistoryService...");
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to verify or create the History table...");
                using var connection = new SqlConnection(config.SQL_SERVER_HISTORY_SERVICE_CONNSTRING);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction(); // rollback is automatic during dispose
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'History')
                    BEGIN
                        CREATE TABLE [dbo].[History]
                        (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [ConversationId] UNIQUEIDENTIFIER NOT NULL,
                            [ActivityId] VARCHAR(100) NOT NULL,
                            [UserId] VARCHAR(50) NOT NULL,
                            [Role] VARCHAR(20) NOT NULL,
                            [Message] NVARCHAR(MAX) NULL,
                            [Intent] VARCHAR(20) NULL,
                            [State] VARCHAR(20) NOT NULL,
                            [Rating] VARCHAR(10) NULL,
                            [Comment] NVARCHAR(MAX) NULL,
                            [Created] DATETIME DEFAULT GETDATE(),
                            [Expiry] DATETIME NOT NULL,
                            [PromptTokenCount] INT,
                            [CompletionTokenCount] INT,
                            [TimeToFirstResponse] INT,
                            [TimeToLastResponse] INT
                        )
                    END
                ";
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                this.logger.LogInformation("successfully verified or created the History table.");
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "verifying or creating the History table raised the following SQL transient exception...");
                return Task.CompletedTask;
            });
        this.logger.LogInformation("successfully started up SqlServerHistoryService.");
    }
}