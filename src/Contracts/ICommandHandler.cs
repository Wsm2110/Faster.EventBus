using System;
using System.Collections.Generic;
using System.Text;

namespace Faster.EventBus.Contracts;

/// <summary>
/// Handles a command and returns a ValueTask of <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TCommand">Type of the command.</typeparam>
/// <typeparam name="TResponse">Type of the response, usually Result.</typeparam>
public interface ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the given command.
    /// </summary>
    ValueTask<TResponse> Handle(TCommand command, CancellationToken ct);
}

