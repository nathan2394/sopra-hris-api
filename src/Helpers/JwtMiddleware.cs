using sopra_hris_api.Helpers;

namespace sopra_hris_api.src.Helpers
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token != null)
                context.Items["User"] = Utility.UserFromToken(token);

            await _next(context);
        }
    }
}
