using System;
using System.Threading;

namespace Faster.EventBus.Contracts;

/// <summary>
/// Represents the main entry point for sending commands within the application.
/// 
/// A command is a request to perform an action (e.g., "CreateOrder", "DeleteUser").
/// Only one command handler should exist for a given command type.
/// 
/// Implementations of this interface are responsible for:
///   - Locating the correct command handler for the command type
///   - Executing any configured pipeline behaviors (logging, validation, etc.)
///   - Returning the result produced by the command handler
///
/// This abstraction decouples application code from the underlying pipeline mechanism.
/// Consumers only need to call <see cref="Send{TResponse}(ICommand{TResponse}, CancellationToken)"/>
/// without caring about the internal execution logic.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Sends a command and asynchronously returns the resulting response.
    /// The appropriate handler is resolved based on the command type.
    /// </summary>
    /// <typeparam name="TResponse">The type of result expected from the command.</typeparam>
    /// <param name="command">The command instance to execute.</param>
    /// <param name="ct">Optional cancellation token to abort execution.</param>
    /// <returns>A <see cref="ValueTask{TResponse}"/> that represents the execution of the command.</returns>
    ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);
}
