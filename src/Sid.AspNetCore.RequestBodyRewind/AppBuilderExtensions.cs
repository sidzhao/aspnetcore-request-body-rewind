using System;
using System.IO;
using Microsoft.AspNetCore.Builder;

namespace Sid.AspNetCore.RequestBodyRewind
{
    public static class AppBuilderExtensions
    {
        public static IApplicationBuilder UseRequestBodyRewind(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.Use(next => async context => {
                // Keep the original stream in a separate
                // variable to restore it later if necessary.
                var stream = context.Request.Body;

                // Optimization: don't buffer the request if
                // there was no stream or if it is rewindable.
                if (stream == Stream.Null || stream.CanSeek)
                {
                    await next(context);

                    return;
                }

                try
                {
                    using (var buffer = new MemoryStream())
                    {
                        // Copy the request stream to the memory stream.
                        await stream.CopyToAsync(buffer);

                        // Rewind the memory stream.
                        buffer.Position = 0L;

                        // Replace the request stream by the memory stream.
                        context.Request.Body = buffer;

                        // Invoke the rest of the pipeline.
                        await next(context);
                    }
                }
                finally
                {
                    // Restore the original stream.
                    context.Request.Body = stream;
                }
            });
        }
    }
}
