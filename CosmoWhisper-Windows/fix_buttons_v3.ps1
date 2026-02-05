$xamlPath = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native\CosmoWhisper-Windows\CosmoWhisper\DashboardWindow.xaml"
$lines = Get-Content $xamlPath

$newLines = @()
$i = 0
while ($i -lt $lines.Count) {
    $line = $lines[$i]
    
    # Check for BtnRestore
    if ($line -match 'x:Name="BtnRestore"') {
        $newLines += '                                                           <Button x:Name="BtnRestore" Content="Restore from Vault" Click="BtnRestore_Click" Padding="15,10" Margin="0,0,10,0" BorderThickness="0">'
        $newLines += '                                                               <Button.Style>'
        $newLines += '                                                                   <Style TargetType="Button">'
        $newLines += '                                                                       <Setter Property="Background" Value="#20FFFFFF"/>'
        $newLines += '                                                                       <Setter Property="Foreground" Value="White"/>'
        $newLines += '                                                                       <Setter Property="Template">'
        $newLines += '                                                                           <Setter.Value>'
        $newLines += '                                                                               <ControlTemplate TargetType="Button">'
        $newLines += '                                                                                   <Border x:Name="border" Background="{TemplateBinding Background}" CornerRadius="10" Padding="{TemplateBinding Padding}">'
        $newLines += '                                                                                       <ContentPresenter x:Name="content" HorizontalAlignment="Center" VerticalAlignment="Center"/>'
        $newLines += '                                                                                   </Border>'
        $newLines += '                                                                                   <ControlTemplate.Triggers>'
        $newLines += '                                                                                       <Trigger Property="IsMouseOver" Value="True">'
        $newLines += '                                                                                           <Setter TargetName="border" Property="Background" Value="White"/>'
        $newLines += '                                                                                           <Setter Property="Foreground" Value="Black"/>'
        $newLines += '                                                                                       </Trigger>'
        $newLines += '                                                                                   </ControlTemplate.Triggers>'
        $newLines += '                                                                               </ControlTemplate>'
        $newLines += '                                                                           </Setter.Value>'
        $newLines += '                                                                       </Setter>'
        $newLines += '                                                                   </Style>'
        $newLines += '                                                               </Button.Style>'
        $newLines += '                                                           </Button>'
        
        # Skip until end of old button
        while ($i -lt $lines.Count -and $lines[$i] -notmatch '</Button>') { $i++ }
        $i++
        continue
    }

    # Check for BtnBackupNow
    if ($line -match 'x:Name="BtnBackupNow"') {
        $newLines += '                                                           <Button x:Name="BtnBackupNow" Content="Back up now" Click="BtnBackupNow_Click" FontWeight="Bold" Padding="20,10" BorderThickness="0">'
        $newLines += '                                                               <Button.Style>'
        $newLines += '                                                                   <Style TargetType="Button">'
        $newLines += '                                                                       <Setter Property="Background" Value="{DynamicResource ThemeAccentBrush}"/>'
        $newLines += '                                                                       <Setter Property="Foreground" Value="White"/>'
        $newLines += '                                                                       <Setter Property="Template">'
        $newLines += '                                                                           <Setter.Value>'
        $newLines += '                                                                               <ControlTemplate TargetType="Button">'
        $newLines += '                                                                                   <Border x:Name="border" Background="{TemplateBinding Background}" CornerRadius="10" Padding="{TemplateBinding Padding}">'
        $newLines += '                                                                                       <ContentPresenter x:Name="content" HorizontalAlignment="Center" VerticalAlignment="Center"/>'
        $newLines += '                                                                                   </Border>'
        $newLines += '                                                                                   <ControlTemplate.Triggers>'
        $newLines += '                                                                                       <Trigger Property="IsMouseOver" Value="True">'
        $newLines += '                                                                                           <Setter TargetName="border" Property="Background" Value="White"/>'
        $newLines += '                                                                                           <Setter Property="Foreground" Value="Black"/>'
        $newLines += '                                                                                       </Trigger>'
        $newLines += '                                                                                   </ControlTemplate.Triggers>'
        $newLines += '                                                                               </ControlTemplate>'
        $newLines += '                                                                           </Setter.Value>'
        $newLines += '                                                                       </Setter>'
        $newLines += '                                                                   </Style>'
        $newLines += '                                                               </Button.Style>'
        $newLines += '                                                           </Button>'
        
        # Skip until end of old button
        while ($i -lt $lines.Count -and $lines[$i] -notmatch '</Button>') { $i++ }
        $i++
        continue
    }

    $newLines += $line
    $i++
}

$newLines | Set-Content $xamlPath -Encoding UTF8
Write-Output "Button styles robustly updated."
