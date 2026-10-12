using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class TokenStorageService
    {
        private readonly IJSRuntime _js;
        private string _cachedToken;

        public TokenStorageService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<string> GetTokenAsync()
        {
            // همیشه از exception handling استفاده می‌کنیم
            // اگر JS در دسترس نباشد (prerendering)، از cache استفاده می‌کنیم
            try
            {
                var token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");
                _cachedToken = token; // cache را به‌روزرسانی کن
                return token ?? string.Empty;
            }
            catch (InvalidOperationException)
            {
                // JS در دسترس نیست (prerendering)
                // این exception طبیعی است و نباید throw شود
                // اگر cache داریم، از آن استفاده کن
                return _cachedToken ?? string.Empty;
            }
            catch (Exception)
            {
                // خطای JS یا سایر خطاها - از cache استفاده کن
                // این exception را log نکن چون در prerendering طبیعی است
                return _cachedToken ?? string.Empty;
            }
        }

        public async Task SetTokenAsync(string token)
        {
            _cachedToken = token;

            try
            {
                if (!string.IsNullOrEmpty(token))
                {
                    await _js.InvokeVoidAsync("localStorage.setItem", "authToken", token);
                }
                else
                {
                    await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
                }
            }
            catch (InvalidOperationException)
            {
                // JS در دسترس نیست (prerendering) - فقط cache را به‌روزرسانی کن
                // این OK است، cache در دسترس است
            }
            catch (Exception)
            {
                // خطای JS یا سایر خطاها - فقط cache را به‌روزرسانی کن
            }
        }

        public async Task RemoveTokenAsync()
        {
            _cachedToken = null;

            try
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
            }
            catch (InvalidOperationException)
            {
                // JS در دسترس نیست (prerendering) - cache قبلاً پاک شده
            }
            catch (Exception)
            {
                // خطای JS یا سایر خطاها - cache قبلاً پاک شده
            }
        }

        public void ClearCache()
        {
            _cachedToken = null;
        }
    }
}

