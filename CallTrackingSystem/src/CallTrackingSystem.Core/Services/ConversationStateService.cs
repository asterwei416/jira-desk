using System.Collections.Concurrent;
using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 對話狀態管理服務（In-Memory）
/// </summary>
public class ConversationStateService : IConversationStateService
{
    private static readonly ConcurrentDictionary<string, ConversationStateDto> _conversations = new();
    private const int TimeoutMinutes = 5;

    public Task StartConversationAsync(string lineUserId)
    {
        var conversation = new ConversationStateDto
        {
            LineUserId = lineUserId,
            CurrentStep = ConversationStep.AwaitingSubject,
            FormData = new CallRecordFormData(),
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _conversations[lineUserId] = conversation;
        return Task.CompletedTask;
    }

    public Task<ConversationStateDto?> GetConversationAsync(string lineUserId)
    {
        _conversations.TryGetValue(lineUserId, out var conversation);
        return Task.FromResult(conversation);
    }

    public Task UpdateConversationAsync(ConversationStateDto conversation)
    {
        conversation = conversation with { LastActivityAt = DateTime.UtcNow };
        _conversations[conversation.LineUserId] = conversation;
        return Task.CompletedTask;
    }

    public Task ClearConversationAsync(string lineUserId)
    {
        _conversations.TryRemove(lineUserId, out _);
        return Task.CompletedTask;
    }

    public Task<List<string>> GetExpiredConversationsAsync()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-TimeoutMinutes);
        var expired = _conversations
            .Where(x => x.Value.LastActivityAt < cutoff)
            .Select(x => x.Key)
            .ToList();

        return Task.FromResult(expired);
    }

    public Task ClearMultipleConversationsAsync(List<string> lineUserIds)
    {
        foreach (var lineUserId in lineUserIds)
        {
            _conversations.TryRemove(lineUserId, out _);
        }

        return Task.CompletedTask;
    }
}
