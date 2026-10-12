namespace MyShop.Core.Models.Ai
{
    public enum AiAudience
    {
        Customer,
        StoreOwner,
        Both
    }

    public record KnowledgeFaq(
        string Intent,
        AiAudience Audience,
        string[] Keywords,
        string Answer);
}
