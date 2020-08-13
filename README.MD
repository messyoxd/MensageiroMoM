# MensageiroMoM

programa que usa RabbitMQ para a persistencia no envio de mensagens para usuarios inativos e GRPC para a troca entre dois usuarios ativos.
#
## SOs Testados

Ubuntu 20
#
## Instalação para o Ubuntu 20
#
1. Dotnet 3.1
```
wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb

sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get update

sudo apt-get install apt-transport-https

sudo apt-get update

sudo apt-get install dotnet-sdk-3.1
```
#
2. Dotnet restore na raiz do projeto
```
dotnet restore
```
#
3. Instalar docker
#
4. Instalar imagem mais recente do RabbitMQ
```
docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

## Executar projeto
#
1. Entre na pasta do projeto MensageiroMoM.GUI
2. Execute:
```
dotnet run
```
