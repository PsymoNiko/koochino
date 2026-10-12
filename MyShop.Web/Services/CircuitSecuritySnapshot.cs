namespace MyShop.Web.Services
{
    /// <summary>
    /// Scoped per Blazor Server circuit. Populated once by <see cref="SecurityContextCircuitHandler"/>
    /// at circuit open, while HttpContext is still available; read by <see cref="SecurityContext"/>
    /// for the rest of the circuit's lifetime, since HttpContext is null once interactive code runs.
    /// </summary>
    public class CircuitSecuritySnapshot
    {
        public int? UserId { get; set; }
        public int? SiteId { get; set; }
        public bool Captured { get; set; }
    }
}
