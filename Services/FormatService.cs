using CardGameCalculator.Models;
using Microsoft.JSInterop;

namespace CardGameCalculator.Services;

public class FormatService(IJSRuntime js)
{
    private const string StorageKey = "selectedFormat";

    public FormatPreset Current { get; private set; } = FormatPreset.Standard;

    public event Action? Changed;

    private Task? _initTask;

    // All callers await the same Task, so the localStorage read happens exactly once
    // even when the layout and a page call Initialize() concurrently.
    public Task Initialize() => _initTask ??= LoadFromStorage();

    private async Task LoadFromStorage()
    {
        var savedName = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (savedName is not null)
        {
            var match = FormatPreset.All.FirstOrDefault(f => f.Name == savedName);
            if (match is not null)
                Current = match;
        }
    }

    public async Task SetFormat(FormatPreset preset)
    {
        Current = preset;
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, preset.Name);
        Changed?.Invoke();
    }
}
