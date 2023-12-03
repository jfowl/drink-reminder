namespace drink_reminder;

public class WindowsBackgroundService : BackgroundService
{
    private readonly ILogger<WindowsBackgroundService> _logger;
    private DateTimeOffset _lastDrinked;
    private readonly TimeSpan _drinkInterval = new(hours: 0, minutes: 30, seconds: 0);

    public WindowsBackgroundService(ILogger<WindowsBackgroundService> logger)
    {
        _logger = logger;
        _lastDrinked = DateTimeOffset.MinValue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.Now;
                var nextDrinkReminder = _lastDrinked + _drinkInterval;

                if (nextDrinkReminder > now)
                {
                    var sleepTime = (int)Math.Max((nextDrinkReminder - now).TotalMilliseconds, 0);

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(
                            "Worker triggered, but not ready yet at: {time}. Next reminder at: {next}, ({ms} ms left)",
                            now, nextDrinkReminder, sleepTime);
                    }
                    await Task.Delay(millisecondsDelay: sleepTime, stoppingToken);
                }
                else
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Worker triggered at: {time}", now);
                    }
                    RemindToDrink();
                    await Task.Delay(millisecondsDelay: 1000, stoppingToken);
                }

            }
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }

    private void RemindToDrink()
    {
        bool locked = Process.GetProcessesByName("logonui").Length != 0;
        if (!locked)
        {
            try
            {
                var dialogResult = MessageBox.Show(
                text: "Trink was!",
                caption: "Erinnerung zu Trinken",
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Exclamation,
                defaultButton: MessageBoxDefaultButton.Button1,
                options: MessageBoxOptions.ServiceNotification
                );

                if (dialogResult == DialogResult.OK)
                {
                    _lastDrinked = DateTimeOffset.Now;
                }
            } catch {
                // Do nothing
            }
        }
    }
}
