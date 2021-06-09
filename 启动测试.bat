:: @echo off

cd ./ZooKeeprDistributedLockSample/bin/Debug/net5.0
start /min "ProducerSeckill" dotnet ZooKeeprDistributedLockSample.dll --minute=35

:: @pause