$asm = [System.Reflection.Assembly]::LoadFrom('C:\Users\user\Desktop\FoxCod\FoxCod.Core\bin\Debug\net10.0\FoxCod.Core.dll')
$actionType = [System.Action[string]]
$log = [System.Delegate]::CreateDelegate($actionType, [System.Console]::WriteLine)
$engineType = $asm.GetType('FoxCod.Core.FoxEngine')
$engine = [System.Activator]::CreateInstance($engineType, $log, $null)
$method = $engineType.GetMethod('ExecuteScript')
$success = $method.Invoke($engine, @('C:\Users\user\Desktop\FoxCod\Scripts\prog.fc', $false))
Write-Host "success=$success"
