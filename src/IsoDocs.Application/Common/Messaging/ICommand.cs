using MediatR;

namespace IsoDocs.Application.Common.Messaging;

/// <summary>
/// 命令介面（CQRS 中的 Command）。表示一個會改變系統狀態的請求。
/// 透過 MediatR 派發到對應的 <see cref="ICommandHandler{TCommand, TResponse}"/>。
/// </summary>
/// <typeparam name="TResponse">命令處理後回傳的結果型別。</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// 不需要回傳值的命令，預設回傳 <see cref="Unit"/>。
/// </summary>
public interface ICommand : IRequest<Unit>
{
}

/// <summary>
/// 命令處理器介面。一個命令對應一個處理器（CQRS 一對一原則）。
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// 不需要回傳值的命令處理器。
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Unit>
    where TCommand : ICommand
{
}
