using Microsoft.AspNetCore.Mvc.RazorPages;
using ChargerControlApp.Services;

public class GrpcStatusModel : PageModel
{
    public string StationState { get; set; }
    public List<string> LogMessages { get; set; }

    public void OnGet()
    {
        // 取得目前站台狀態
        // 這裡假設你有辦法取得 SlotServices 實例
        // 若 SlotServices 是 Singleton，可用 DI 注入
        StationState = SwappingStationService.GetCurrentStationState();

        // 取得最新 100 筆 log
        LogMessages = SwappingStationService.LogMessages.Reverse().Take(100).ToList();
    }
}