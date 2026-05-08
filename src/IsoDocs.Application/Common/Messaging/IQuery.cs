using MediatR;

namespace IsoDocs.Application.Common.Messaging;

/// <summary>
/// 查詢介面（CQRS 中的 Query）。表示一個只讀取系統狀態的請求，不應產生副作用。
/// </summary>
/// <typeparam name="TResponse">查詢回傳的型別。</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// 查詢處理器介面。一個查詢對應一個處理器。
/// 查詢處理器應該避免修改資料庫狀態。
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
