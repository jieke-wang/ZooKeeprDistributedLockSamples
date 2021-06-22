:: @echo off

:: dotnet ZooKeeprDistributedLockWebSample.dll --urls="http://*:5000"
cd ./ZooKeeprDistributedLockWebSample/bin/Debug/net5.0
start "ProducerSeckillServer" dotnet ZooKeeprDistributedLockWebSample.dll

:: @pause