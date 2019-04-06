set top=C:\Users\mizpe\source\repos\ASCOM.Wise40

cd "%top%\SafeToOperate\bin\x86\Debug"
c:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm /codebase /tlb "%top%\SafeToOperate\bin\x86\Debug\ASCOM.Wise40SafeToOperate.SafetyMonitor.dll"

cd "%top%\Boltwood\bin\x86\Debug"
c:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm /codebase /tlb "%top%\Boltwood\bin\x86\Debug\ASCOM.Wise40.Boltwood.ObservingConditions.dll"

cd "%top%\ComputerControl\bin\x86\Debug"
c:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm /codebase /tlb "%top%\ComputerControl\bin\x86\Debug\ASCOM.Wise40.ComputerControl.SafetyMonitor.dll"

cd "%top%\Dome\bin\x86\Debug"
c:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm /codebase /tlb "%top%\Dome\bin\x86\Debug\ASCOM.Wise40.Dome.dll"

cd "%top%\FilterWheel\bin\x86\Debug"
c:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm /codebase /tlb "%top%\FilterWheel\bin\x86\Debug\ASCOM.Wise40.FilterWheel.dll"

cd "%top%\Focus\bin\x86\Debug"
c:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm /codebase /tlb "%top%\Focus\bin\x86\Debug\ASCOM.Wise40.Focuser.dll"

cd "%top%\Telescope\bin\x86\Debug"
c:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm /codebase /tlb "%top%\Telescope\bin\x86\Debug\ASCOM.Wise40.Telescope.dll"

cd "%top%\VantagePro\bin\x86\Debug"
c:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm /codebase /tlb "%top%\VantagePro\bin\x86\Debug\ASCOM.Wise40.VantagePro.ObservingConditions.dll"
