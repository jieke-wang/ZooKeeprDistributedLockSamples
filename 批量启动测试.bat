:: @echo off

cd ./ZooKeeprDistributedLockSample/bin/Debug/net5.0

set n=0
:loop

set /a n+=1

start /min "ProducerSeckill%n%" dotnet ZooKeeprDistributedLockSample.dll --minute=36

if %n% == 3 (
:: @pause
exit
)

goto loop

:: @pause