:: @echo off

cd ./SeckillConsoleClient/bin/Debug/net5.0

set n=0
:loop

set /a n+=1

:: start /min "ProducerSeckill%n%" dotnet SeckillConsoleClient.dll
start "ProducerSeckill%n%" dotnet SeckillConsoleClient.dll

if %n% == 1 (
:: @pause
exit
)

goto loop

:: @pause