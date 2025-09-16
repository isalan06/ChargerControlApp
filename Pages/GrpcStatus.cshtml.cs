using Microsoft.AspNetCore.Mvc.RazorPages;
using ChargerControlApp.Services;

public class GrpcStatusModel : PageModel
{
    public string StationState { get; set; }
    public List<string> LogMessages { get; set; }

    public void OnGet()
    {
        // ���o�ثe���x���A
        // �o�̰��]�A����k���o SlotServices ���
        // �Y SlotServices �O Singleton�A�i�� DI �`�J
        StationState = SwappingStationService.GetCurrentStationState();

        // ���o�̷s 100 �� log
        LogMessages = SwappingStationService.LogMessages.Reverse().Take(100).ToList();
    }
}