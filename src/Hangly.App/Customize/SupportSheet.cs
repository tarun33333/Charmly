//
//  SupportSheet.cs
//  Hangly
//

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Hangly.App.Customize;

internal static class SupportSheet
{
    /// <summary>Shows the sheet over <paramref name="root"/> (no-op in Hangly-Tarun).</summary>
    public static Task ShowAsync(FrameworkElement root) => Task.CompletedTask;

    /// <summary>TextBlock helper for layout compatibility.</summary>
    public static TextBlock MessageBlock(double fontSize = 18, bool balanced = false)
    {
        string message = balanced ? Hangly.Core.Settings.SupportCard.BalancedMessage : Hangly.Core.Settings.SupportCard.Message;
        string amount = Hangly.Core.Settings.SupportCard.Amount;
        int at = message.IndexOf(amount, StringComparison.Ordinal);

        var block = new TextBlock
        {
            FontSize = fontSize,
            LineHeight = fontSize * 1.35,
            Opacity = 0.88,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(block, "SupportMessage");
        if (at >= 0)
        {
            block.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = message[..at] });
            block.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = amount, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
            block.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = message[(at + amount.Length)..] });
        }
        else
        {
            block.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = message });
        }
        return block;
    }
}
