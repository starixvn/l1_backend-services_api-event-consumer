using System.Threading.Tasks;
using ApiEventConsumer.Core.Models.ChatBot;
using Refit;

namespace ApiEventConsumer.Subscribers
{
    public interface IChatBotApi
    {
        [Post("/forwardwithtoken")]
        Task<ChatBotResult> ForwardMessage(string token, string conversationId, string message);
    }
} 