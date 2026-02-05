$xamlPath = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native\CosmoWhisper-Windows\CosmoWhisper\DashboardWindow.xaml"
$lines = Get-Content $xamlPath

$newLines = @()
$i = 0
while ($i -lt $lines.Count) {
    $line = $lines[$i]
    
    if ($line -match 'x:Name="BtnRestore"|x:Name="BtnBackupNow"') {
        $isBackup = $line -match 'BtnBackupNow'
        $name = if ($isBackup) { "BtnBackupNow" } else { "BtnRestore" }
        $content = if ($isBackup) { "Back up now" } else { "Restore from Vault" }
        $click = if ($isBackup) { "BtnBackupNow_Click" } else { "BtnRestore_Click" }
        $margin = if ($isBackup) { "" } else { " Margin=""0,0,10,0""" }
        $bg = if ($isBackup) { "{DynamicResource ThemeAccentBrush}" } else { "#20FFFFFF" }
        $fontWeight = if ($isBackup) { "FontWeight=""Bold""" } else { "" }
        
        $newLines += "                                                           <Button x:Name=""$name"" Content=""$content"" Click=""$click""$fontWeight Padding=""15,10""$margin BorderThickness=""0"">"
        $newLines += '                                                               <Button.Style>'
        $newLines += '                                                                   <Style TargetType="Button">'
        $newLines += "                                                                       <Setter Property=""Background"" Value=""$bg""/>"
        $newLines += '                                                                       <Setter Property="Foreground" Value="White"/>'
        $newLines += '                                                                       <Setter Property="Template">'
        $newLines += '                                                                           <Setter.Value>'
        $newLines += '                                                                               <ControlTemplate TargetType="Button">'
        $newLines += '                                                                                   <Border x:Name="border" Background="{TemplateBinding Background}" CornerRadius="10" Padding="{TemplateBinding Padding}">'
        $newLines += '                                                                                       <TextBlock x:Name="btnText" Text="{TemplateBinding Content}" '
        $newLines += '                                                                                                  Foreground="{TemplateBinding Foreground}" '
        $newLines += '                                                                                                  HorizontalAlignment="Center" VerticalAlignment="Center"/>'
        $newLines += '                                                                                   </Border>'
        $newLines += '                                                                                   <ControlTemplate.Triggers>'
        $newLines += '                                                                                       <Trigger Property="IsMouseOver" Value="True">'
        $newLines += '                                                                                           <Setter TargetName="border" Property="Background" Value="White"/>'
        $newLines += '                                                                                           <Setter TargetName="btnText" Property="Foreground" Value="Black"/>'
        $newLines += '                                                                                       </Trigger>'
        $newLines += '                                                                                   </ControlTemplate.Triggers>'
        $newLines += '                                                                               </ControlTemplate>'
        $newLines += '                                                                           </Setter.Value>'
        $newLines += '                                                                       </Setter>'
        $newLines += '                                                                   </Style>'
        $newLines += '                                                               </Button.Style>'
        $newLines += '                                                           </Button>'
        
        while ($i -lt $lines.Count -and $lines[$i] -notmatch '</Button>') { $i++ }
        $i++
        continue
    }

    $newLines += $line
    $i++
}

$newLines | Set-Content $xamlPath -Encoding UTF8
Write-Output "Button styles fixed with explicit TextBlock for hover contrast."
