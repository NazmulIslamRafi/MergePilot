COLOR SCHEME INTEGRATION - Based on Bash Script

Extracted from your bash script color definitions:
- GREEN (\e[32m)   ? #22C55E (Success)
- YELLOW (\e[33m)  ? #EAB308 (Warning/Skipped)
- RED (\e[31m)     ? #EF4444 (Error/Failed)
- BLUE (\e[34m)    ? #3B82F6 (Info/General)
- Secondary Blue   ? #60A5FA (Alternative Info)

NEW FILES CREATED:
================

1. ColorScheme.cs - Central color constants repository
   - Provides named colors matching bash script palette
   - SolidColorBrush objects ready for WPF usage
   - Hex values for reference

2. Updated LogFormatter.cs - Color helper methods
   - SuccessText(string) ? GREEN colored text
   - WarningText(string) ? YELLOW colored text
   - ErrorText(string) ? RED colored text
   - InfoText(string) ? BLUE colored text
   - ColoredText(string, color) ? Generic coloring

3. Updated MainWindow.xaml - XAML Resources
   - Color definitions: SuccessGreenColor, WarningYellowColor, ErrorRedColor, InfoBlueColor, SecondaryBlueColor
   - Brush resources: SuccessGreenBrush, WarningYellowBrush, ErrorRedBrush, InfoBlueBrush, SecondaryBlueBrush

USAGE EXAMPLES:
===============

In C# Code:
-----------
// For direct color access
var successBrush = ColorScheme.SuccessBrush;  // Green for success
var errorBrush = ColorScheme.ErrorBrush;      // Red for errors
var warningBrush = ColorScheme.WarningBrush;  // Yellow for warnings
var infoBrush = ColorScheme.InfoBrush;        // Blue for info

// For formatted log output
var (text, color) = LogFormatter.SuccessText("Operation completed!");
var (text, color) = LogFormatter.ErrorText("Operation failed!");
var (text, color) = LogFormatter.WarningText("Operation skipped");
var (text, color) = LogFormatter.InfoText("Operation in progress...");

In XAML:
--------
<!-- Use the predefined brush resources -->
<TextBlock Foreground="{StaticResource SuccessGreenBrush}" Text="Success!"/>
<TextBlock Foreground="{StaticResource ErrorRedBrush}" Text="Error!"/>
<TextBlock Foreground="{StaticResource WarningYellowBrush}" Text="Warning!"/>
<TextBlock Foreground="{StaticResource InfoBlueBrush}" Text="Info!"/>

MAPPING TO LOG OUTPUT:
=====================

? SUCCESSFUL MERGES:    ? GREEN (#22C55E)
?  SKIPPED (Already Merged): ? YELLOW (#EAB308)
? FAILED OPERATIONS:    ? RED (#EF4444)
?? Progress/Info:        ? BLUE (#3B82F6)

This color scheme directly mirrors your bash script for consistency across all platforms!
