using System.Threading.Tasks;
using FutronicService.Models;

namespace FutronicService.Services
{
    public interface IFingerprintService
    {
        Task<ApiResponse<CaptureResponseData>> CaptureAsync(CaptureRequest request);
        Task<ApiResponse<RegisterResponseData>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<VerifyResponseData>> VerifyAsync(VerifyRequest request);
        Task<ApiResponse<IdentifyResponseData>> IdentifyAsync(IdentifyRequest request);
        Task<ApiResponse<HealthResponseData>> GetHealthAsync();
        ApiResponse<ConfigResponseData> GetConfig();
        ApiResponse<ConfigResponseData> UpdateConfig(UpdateConfigRequest request);
        bool IsDeviceConnected();

        // Métodos optimizados
        Task<ApiResponse<VerifySimpleResponseData>> VerifySimpleAsync(VerifySimpleRequest request);
        Task<ApiResponse<RegisterMultiSampleResponseData>> RegisterMultiSampleAsync(RegisterMultiSampleRequest request);
        Task<ApiResponse<IdentifyLiveResponseData>> IdentifyLiveAsync(IdentifyLiveRequest request);
    }
}
