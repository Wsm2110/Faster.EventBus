using System;
using System.Collections.Generic;
using System.Text;

namespace Faster.EventBus.Contracts;

/// <summary>
/// Represents a command that expects a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the response, usually Result.</typeparam>
public interface ICommand<TResponse> { }

