using System;
using System.Collections.Generic;
using System.Text;

public interface IUseCase<TResponse>
{
}

public interface IUseCase<TRequest, TResponse> : IUseCase<TResponse>
{
    TRequest Request { get; }
}
