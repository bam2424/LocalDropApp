using Microsoft.Maui.ApplicationModel;
using MauiApp = Microsoft.Maui.Controls.Application;

namespace LocalDropApp.Services;

public interface IFirstRunSetupService
{
    Task<bool> IsFirstRunAsync();
    Task<string?> SetupDownloadFolderAsync();
    Task CompleteFirstRunAsync();
}

public class FirstRunSetupService : IFirstRunSetupService
{
    private const string FIRST_RUN_KEY = "IsFirstRun";
    
    public async Task<bool> IsFirstRunAsync()
    {
        await Task.Delay(10); // Make it async for consistency
        return !Preferences.Get(FIRST_RUN_KEY, false);
    }

    public async Task<string?> SetupDownloadFolderAsync()
    {
        try
        {
            // Get the default path
            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "LocalDrop Downloads");

            // Ask the user if they want to use the default path or choose a custom one
            var useDefault = await Application.Current!.MainPage!.DisplayAlert(
                "Setup Download Folder",
                $"LocalDrop needs a folder to save downloaded files.\n\nWould you like to use the default location?\n\n📁 {defaultPath}",
                "Use Default",
                "Choose Custom");

            string selectedPath;
            
            if (useDefault)
            {
                selectedPath = defaultPath;
            }
            else
            {
                // For now, let the user manually enter a path
                // In a full implementation, you'd use a folder picker dialog
                var customPath = await Application.Current.MainPage.DisplayPromptAsync(
                    "Custom Download Folder",
                    "Enter the full path where you want to save downloaded files:",
                    "OK",
                    "Cancel",
                    placeholder: defaultPath,
                    initialValue: defaultPath);

                if (string.IsNullOrWhiteSpace(customPath))
                {
                    // User cancelled, use default
                    selectedPath = defaultPath;
                }
                else
                {
                    selectedPath = customPath.Trim();
                }
            }

            // Validate and create the directory
            try
            {
                if (!Directory.Exists(selectedPath))
                {
                    Directory.CreateDirectory(selectedPath);
                }

                // Test write access by creating a temporary file
                var testFile = Path.Combine(selectedPath, ".localdrop_test");
                await File.WriteAllTextAsync(testFile, "test");
                File.Delete(testFile);

                // Success
                await Application.Current.MainPage.DisplayAlert(
                    "Download Folder Ready",
                    $"✅ Download folder has been set up successfully:\n\n📁 {selectedPath}",
                    "OK");

                return selectedPath;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Folder Setup Error",
                    $"❌ Could not create or access the folder:\n{selectedPath}\n\nError: {ex.Message}\n\nThe default Documents folder will be used instead.",
                    "OK");

                // Fallback to Documents folder
                var fallbackPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return fallbackPath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SetupDownloadFolderAsync: {ex.Message}");
            
            // Ultimate fallback
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }

    public async Task CompleteFirstRunAsync()
    {
        await Task.Delay(10); // Make it async for consistency
        Preferences.Set(FIRST_RUN_KEY, true);
    }
} 
