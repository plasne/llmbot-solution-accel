using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Iso8601DurationHelper;
using Microsoft.Extensions.Logging;
using NetBricks;
using Polly;
using Shared;
using Shared.Models.Memory;

namespace Memory;

public class SqlServerMemoryStore(
    IConfig config,
    DefaultAzureCredential defaultAzureCredential,
    ILogger<SqlServerMemoryStore> logger)
: MemoryStoreBase, IMemoryStore
{
    private readonly IConfig config = config;
    private readonly DefaultAzureCredential defaultAzureCredential = defaultAzureCredential;
    private readonly ILogger<SqlServerMemoryStore> logger = logger;

    private SqlConnection GetConnection()
    {
        // if the connection string contains a password, use it as is
        if (this.config.SQL_SERVER_HISTORY_SERVICE_CONNSTRING.Contains("Password=", StringComparison.OrdinalIgnoreCase))
        {
            return new SqlConnection(this.config.SQL_SERVER_HISTORY_SERVICE_CONNSTRING);
        }

        // use an access token
        var context = new Azure.Core.TokenRequestContext(["https://database.windows.net/.default"]);
        var connection = new SqlConnection(config.SQL_SERVER_HISTORY_SERVICE_CONNSTRING)
        {
            AccessToken = this.defaultAzureCredential.GetToken(context).Token
        };
        return connection;
    }

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

    public async Task<Guid> StartGenerationAsync(Interaction request, Interaction response, Duration expiry, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForStartGeneration(request);
        base.ValidateInteractionForStartGeneration(response);
        var conversationId = Guid.Empty;
        try
        {
            await this.ExecuteWithRetryOnTransient(
                async () =>
                {
                    this.logger.LogDebug("attempting to insert interaction for user {u} into the history database...", request.UserId);
                    using var connection = this.GetConnection();
                    await connection.OpenAsync();
                    using var command = connection.CreateCommand();
                    using var transaction = await connection.BeginTransactionAsync(); // rollback is automatic during dispose
                    command.Transaction = (SqlTransaction)transaction;
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

                        SELECT @conversationId;
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
                    command.Parameters.AddWithValue("@expiry", DateTime.UtcNow + expiry);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        conversationId = reader.GetGuid(0);
                        request.ConversationId = conversationId;
                        response.ConversationId = conversationId;
                    }
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
                throw new HttpException(423, "a response is already being generated.");
            }
            throw;
        }
        return conversationId;
    }

    public async Task CompleteGenerationAsync(Interaction response, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForCompleteGeneration(response);
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to complete interaction for user {u} into the history database...", response.UserId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    UPDATE [dbo].[History]
                    SET [ConversationId] = @conversationId, [Message] = @message, [State] = @state, [Intent] = @intent,
                        [PromptTokenCount] = @promptTokenCount, [CompletionTokenCount] = @completionTokenCount,
                        [TimeToFirstResponse] = @timeToFirstResponse, [TimeToLastResponse] = @timeToLastResponse
                    WHERE [UserId] = @userId AND [ActivityId] = @activityId;
                ";
                command.Parameters.AddWithValue("@conversationId", response.ConversationId);
                command.Parameters.AddWithValue("@userId", response.UserId);
                command.Parameters.AddWithValue("@activityId", response.ActivityId);
                command.Parameters.AddWithValue("@message", response.Message ?? "");
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

    public async Task ChangeConversationTopicAsync(Interaction changeTopic, Duration expiry, CancellationToken cancellationToken = default)
    {
        base.ValidateInteractionForTopicChange(changeTopic);
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to change conversation for user {u} in the history database...", changeTopic.UserId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    INSERT INTO [dbo].[History]
                        ([ConversationId], [ActivityId], [UserId], [Role], [State], [Intent], [Expiry])
                    VALUES
                        (@conversationId, @activityId, @userId, @role, @state, @intent, @expiry);
                ";
                command.Parameters.AddWithValue("@conversationId", changeTopic.ConversationId);
                command.Parameters.AddWithValue("@activityId", changeTopic.ActivityId);
                command.Parameters.AddWithValue("@userId", changeTopic.UserId);
                command.Parameters.AddWithValue("@role", changeTopic.Role.ToString().ToUpper());
                command.Parameters.AddWithValue("@state", changeTopic.State.ToString().ToUpper());
                command.Parameters.AddWithValue("@intent", changeTopic.Intent.ToString().ToUpper());
                command.Parameters.AddWithValue("@expiry", DateTime.UtcNow + expiry);
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                this.logger.LogInformation("successfully changed conversation for user {u} in the history database.", changeTopic.UserId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "verifying or creating the History table raised the following SQL transient exception...");
                return Task.CompletedTask;
            });
    }

    public async Task ClearLastFeedbackAsync(string userId, CancellationToken cancellationToken = default)
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to update user {u} feedback in the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(cancellationToken); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    UPDATE [dbo].[History]
                    SET [Comment] = NULL, [Rating] = NULL
                    WHERE Id = (SELECT MAX(Id) FROM [dbo].[History] WHERE [Role] = 'ASSISTANT' AND [UserId] = @userId);
                ";

                command.Parameters.AddWithValue("@userId", userId);

                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                this.logger.LogInformation("successfully update user {u} feedback in the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "update feedback for user {u} message raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
    }

    public async Task ClearFeedbackAsync(string userId, string activityId, CancellationToken cancellationToken = default)
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to update user {u} feedback in the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(cancellationToken); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    UPDATE [dbo].[History]
                    SET [Comment] = NULL, [Rating] = NULL
                    WHERE [UserId] = @userId AND [ActivityId] = @activityId AND [Role] = 'ASSISTANT';
                ";

                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@activityId", activityId);

                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                this.logger.LogInformation("successfully update user {u} feedback in the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "update feedback for user {u} message raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
    }

    public async Task CommentOnLastMessageAsync(string userId, string comment, CancellationToken cancellationToken = default)
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to update user {u} comment in the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(cancellationToken); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    UPDATE [dbo].[History]
                    SET [Comment] = @comment
                    WHERE Id = (SELECT MAX(Id) FROM [dbo].[History] WHERE [Role] = 'ASSISTANT' AND [UserId] = @userId);
                ";
                command.Parameters.AddWithValue("@comment", comment);
                command.Parameters.AddWithValue("@userId", userId);

                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                this.logger.LogInformation("successfully update user {u} comment in the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "update comment for user {u} message raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
    }

    public async Task CommentOnMessageAsync(string userId, string activityId, string comment, CancellationToken cancellationToken = default)
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to update user {u} comment in the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(cancellationToken); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    UPDATE [dbo].[History]
                    SET [Comment] = @comment
                    WHERE [UserId] = @userId AND [ActivityId] = @activityId AND [Role] = 'ASSISTANT';
                ";
                command.Parameters.AddWithValue("@comment", comment);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@activityId", activityId);

                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                this.logger.LogInformation("successfully update user {u} comment in the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "update comment for user {u} message raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
    }

    public async Task DeleteActivitiesAsync(string userId, int count = 1, CancellationToken cancellationToken = default)
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to delete user {u} message in the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(cancellationToken); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    UPDATE [dbo].[History]
                    SET [State] = 'DELETED', [Message] = NULL
                    WHERE Id IN (SELECT TOP (@count) Id FROM [dbo].[History] WHERE [UserId] = @userId ORDER BY Id DESC);
                ";
                command.Parameters.AddWithValue("@count", count);
                command.Parameters.AddWithValue("@userId", userId);

                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                this.logger.LogInformation("successfully delete user {u} message in the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "delete message for user {u} message raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
    }

    public async Task DeleteActivityAsync(string userId, string activityId, CancellationToken cancellationToken = default)
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to delete user {u} message in the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(cancellationToken); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    UPDATE [dbo].[History]
                    SET [State] = 'DELETED', [Message] = NULL
                    WHERE [UserId] = @userId AND [ActivityId] = @activityId;
                ";
                command.Parameters.AddWithValue("@activityId", activityId);
                command.Parameters.AddWithValue("@userId", userId);

                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                this.logger.LogInformation("successfully delete user {u} message in the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "delete message for user {u} message raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
    }

    public async Task<Conversation> GetLastConversationAsync(string userId, int? maxTokens, string? modelName, CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation { Id = Guid.Empty, Turns = [] };
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to get the current conversation for user {u} from the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT [ConversationId], [Role], [Message]
                    FROM [dbo].[History]
                    WHERE [ConversationId] IN (
                        SELECT TOP 1 [ConversationId]
                        FROM [dbo].[History]
                        WHERE [UserId] = @userId
                        ORDER BY [Id] DESC)
                    AND [Expiry] > GETDATE()
                    ORDER BY [Id] ASC;

                    SELECT [Prompt] FROM [dbo].[CustomInstructions]
                    WHERE [UserId] = @userId;
                ";
                command.Parameters.AddWithValue("@userId", userId);
                using var reader = await command.ExecuteReaderAsync();
                var conversationIdOrdinal = reader.GetOrdinal("ConversationId");
                var roleOrdinal = reader.GetOrdinal("Role");
                var messageOrdinal = reader.GetOrdinal("Message");
                int totalTokenCount = 0;
                while (await reader.ReadAsync())
                {
                    conversation.Id = reader.GetGuid(conversationIdOrdinal);
                    var turn = new Turn
                    {
                        Msg = string.Empty,
                        Role = reader.GetString(roleOrdinal).AsEnum(() => Roles.UNKNOWN),
                    };
                    if (!await reader.IsDBNullAsync(messageOrdinal))
                    {
                        turn.Msg = reader.GetString(messageOrdinal);
                    }
                    if (!string.IsNullOrWhiteSpace(turn.Msg))
                    {
                        if (modelName is not null && maxTokens is not null && IsMaxTokenLimitExceeded(modelName, maxTokens.Value, turn.Msg, ref totalTokenCount))
                        {
                            break;
                        }

                        conversation.Turns.Add(turn);
                    }
                }
                await reader.NextResultAsync();
                if (await reader.ReadAsync() && !await reader.IsDBNullAsync(0))
                {
                    conversation.CustomInstructions = reader.GetString(0);
                }
                this.logger.LogInformation(
                    "successfully obtained current conversation for user {u} from the history database containing {n} turns.",
                    userId,
                    conversation.Turns.Count);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "getting the current conversation for user {u} raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
        return conversation;
    }

    public async Task RateLastMessageAsync(string userId, string rating, CancellationToken cancellationToken = default)
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to update user {u} rating in the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(cancellationToken); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    UPDATE [dbo].[History]
                    SET [Rating] = @rating
                    WHERE Id = (SELECT MAX(Id) FROM [dbo].[History] WHERE [Role] = 'ASSISTANT' AND [UserId] = @userId);
                ";
                command.Parameters.AddWithValue("@rating", rating);
                command.Parameters.AddWithValue("@userId", userId);

                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                this.logger.LogInformation("successfully update user {u} rating in the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "update rating for user {u} message raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
    }

    public async Task RateMessageAsync(string userId, string activityId, string rating, CancellationToken cancellationToken = default)
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to update user {u} rating in the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(cancellationToken); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    UPDATE [dbo].[History]
                    SET [Rating] = @rating
                    WHERE [UserId] = @userId AND [ActivityId] = @activityId AND [Role] = 'ASSISTANT';
                ";
                command.Parameters.AddWithValue("@rating", rating);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@activityId", activityId);

                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                this.logger.LogInformation("successfully update user {u} rating in the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "update rating for user {u} message raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
    }

    public async Task SetCustomInstructionsAsync(string userId, CustomInstructions instructions, CancellationToken cancellationToken = default)
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to upsert custom instructions for user {u} into the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    MERGE [dbo].[CustomInstructions] AS target
                    USING (SELECT @userId, @prompt) AS source ([UserId], [Prompt])
                    ON (target.[UserId] = source.[UserId])
                    WHEN MATCHED THEN
                        UPDATE SET [Prompt] = source.[Prompt]
                    WHEN NOT MATCHED THEN
                        INSERT ([UserId], [Prompt])
                        VALUES (source.[UserId], source.[Prompt]);
                ";
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@prompt", instructions.Prompt);
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                this.logger.LogInformation("successfully upserted custom instructions for user {u} into the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "upserting custom instructions for user {u} raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
    }

    public async Task DeleteCustomInstructionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to delete custom instructions for user {u} in the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    DELETE FROM [dbo].[CustomInstructions]
                    WHERE [UserId] = @userId;
                ";
                command.Parameters.AddWithValue("@userId", userId);
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                this.logger.LogInformation("successfully deleted custom instructions for user {u} in the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "deleting custom instructions for user {u} raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
    }

    public async Task<CustomInstructions> GetCustomInstructionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        CustomInstructions instructions = new() { Prompt = string.Empty };
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to get custom instructions for user {u} from the history database...", userId);
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT [Prompt] FROM [dbo].[CustomInstructions]
                    WHERE [UserId] = @userId;
                ";
                command.Parameters.AddWithValue("@userId", userId);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync() && !await reader.IsDBNullAsync(0))
                {
                    instructions.Prompt = reader.GetString(0);
                }
                this.logger.LogInformation("successfully obtained custom instructions for user {u} from the history database.", userId);
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "getting the custom instructions for user {u} raised the following SQL transient exception...", userId);
                return Task.CompletedTask;
            });
        return instructions;
    }

    public async Task ProvisionAsync()
    {
        await this.ExecuteWithRetryOnTransient(
            async () =>
            {
                this.logger.LogDebug("attempting to verify or create the SQL resources...");
                using var connection = this.GetConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                using var transaction = await connection.BeginTransactionAsync(); // rollback is automatic during dispose
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables t
                                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                                    WHERE t.name = 'History' AND s.name = 'dbo')
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
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_History_Start' AND object_id = OBJECT_ID('dbo.History'))
                    BEGIN
                        CREATE INDEX [idx_History_Start]
                        ON [dbo].[History] ([UserId], [Id] DESC);
                    END

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_History_Complete' AND object_id = OBJECT_ID('dbo.History'))
                    BEGIN
                        CREATE INDEX [idx_History_Complete]
                        ON [dbo].[History] ([UserId], [ActivityId]);
                    END

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_History_Current' AND object_id = OBJECT_ID('dbo.History'))
                    BEGIN
                        CREATE INDEX [idx_History_Current]
                        ON [dbo].[History] ([ConversationId], [State], [Expiry], [Id]);
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables t
                                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                                    WHERE t.name = 'CustomInstructions' AND s.name = 'dbo')
                    BEGIN
                        CREATE TABLE [dbo].[CustomInstructions]
                        (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [UserId] VARCHAR(50) NOT NULL,
                            [Prompt] NVARCHAR(MAX) NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_CustomInstructions_UserId' AND object_id = OBJECT_ID('dbo.CustomInstructions'))
                    BEGIN
                        CREATE INDEX [idx_CustomInstructions_UserId]
                        ON [dbo].[CustomInstructions] ([UserId]);
                    END
                ";
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                this.logger.LogInformation("successfully verified or created the SQL resources.");
            }, (ex, _) =>
            {
                this.logger.LogError(ex, "verifying or creating the CustomInstructions table raised the following SQL transient exception...");
                return Task.CompletedTask;
            });
    }
}