# CoreMDFe 🚚

O **CoreMDFe** é um sistema emissor de Manifesto Eletrônico de Documentos Fiscais (MDF-e) moderno, rápido e multiplataforma. Construído com foco em usabilidade (Heurísticas de Nielsen), ele suporta emissão normal, carregamento posterior, controle multi-empresa (Multi-Tenant) e atualizações automáticas silenciosas.

---

## ✨ Principais Funcionalidades

* **Emissão de MDF-e:** Suporte completo ao modal Rodoviário, com adição de NF-e, CT-e, controle de CIOT, Vale Pedágio e Seguros.
* **Carregamento Posterior:** Inclusão de DF-e em lote com controle inteligente de numeração de sequencial (Sefaz).
* **Multi-Empresa (Multi-Tenant):** Bancos de dados SQLite isolados por empresa.
* **Auto-Atualização (Velopack):** O sistema se atualiza sozinho sem a necessidade de intervenção do usuário, baixando pacotes diretamente do GitHub Releases.
* **Multiplataforma:** Roda nativamente em Windows e Linux.
* **Logs Inteligentes:** Integração com Serilog para registro de eventos e erros em arquivos rotativos (`.txt`).
* **Prevenção de Múltiplas Instâncias:** Uso de Global Mutex para evitar corrupção de banco de dados.
* **Auto-Limpeza:** Rotinas de background para manter o disco do cliente livre de XMLs intermediários inúteis e manifestos muito antigos.

---

## 🛠️ Tecnologias e Dependências

* **Framework Base:** [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
* **Interface Gráfica:** [Avalonia UI](https://avaloniaui.net/) (XAML Multiplataforma)
* **Arquitetura:** MVVM (CommunityToolkit.Mvvm) + CQRS ([MediatR](https://github.com/jbogard/MediatR))
* **Banco de Dados:** [SQLite](https://www.sqlite.org/) via Entity Framework Core (EF Core)
* **Comunicação SEFAZ:** Biblioteca [Zeus/DFe (.NET)](https://github.com/ZeusAutomacao/DFe.NET)
* **Distribuição/Update:** [Velopack](https://velopack.io/)
* **Logs:** [Serilog](https://serilog.net/) (Sinks: Console e File)

---

## ⚙️ Configurando o Ambiente de Desenvolvimento

Para rodar e compilar este projeto na sua máquina, você precisará instalar algumas ferramentas globais.

### 1. Pré-requisitos
* Instale o **.NET 10 SDK**.
* Instale a ferramenta do Entity Framework Core (para lidar com o banco de dados):

```bash
dotnet tool install --global dotnet-ef
```

* Instale a CLI do Velopack (para empacotamento e distribuição):

```bash
dotnet tool install -g vpk
```

### 2. Rodando o Projeto

Clone o repositório, restaure os pacotes e inicie o projeto Desktop:

```bash
git clone https://github.com/Hacerfak/CoreMDFeApp.git
cd CoreMDFeApp/CoreMDFe.Desktop
dotnet restore
dotnet run

```

Se você for compilar e gerar as *Releases* usando um ambiente Linux, o sistema precisa de algumas bibliotecas de desenvolvimento e o `vpk` precisa de ferramentas do sistema operacional para manipular os pacotes AppImage. Instale-as via terminal:

```bash
# Em distribuições baseadas em Debian/Ubuntu:
sudo apt update
sudo apt install squashfs-tools libgdiplus libc6-dev

```

### 3. Migrations (Banco de Dados)

Sempre que alterar uma entidade no projeto `CoreMDFe.Core`, você deve gerar uma nova migration na camada de infraestrutura:

```bash
cd CoreMDFe.Infrastructure
dotnet ef migrations add NomeDaSuaAlteracao --startup-project ../CoreMDFe.Desktop

```

*Nota: A aplicação do banco (Update) é feita automaticamente pelo sistema em tempo de execução via `DbContext.Database.MigrateAsync()` no momento em que o cliente loga na empresa (Tenant).*

---

## 📦 Compilação e Distribuição (Release)

O processo de publicação utiliza o **Velopack** para gerar instaladores e arquivos de atualização diferencial (Delta). Siga o fluxo abaixo sempre que for lançar uma nova versão.

⚠️ **A Regra de Ouro do Velopack:** O empacotamento final (`vpk pack`) **DEVE** ser feito no mesmo sistema operacional de destino. Para gerar o instalador do Windows, você deve rodar o comando no Windows. Para gerar o instalador do Linux, você deve rodar o comando no Linux.

### Passo 1: Atualizar a Versão Oficial

Abra o arquivo `CoreMDFe.Desktop/CoreMDFe.Desktop.csproj` e atualize a tag de versão. Esta é a "Fonte da Verdade" do sistema.

```xml
<PropertyGroup>
    <Version>1.0.2</Version> 
</PropertyGroup>

```

### Passo 2: Empacotar para WINDOWS

Abra o terminal (Powershell/CMD), navegue até a pasta `CoreMDFe.Desktop` e execute:

1. **Publicar (CMD):**

```bash
set DOTNET_ROLL_FORWARD=Major
vpk pack -u CoreMDFe -v 1.0.2 -p .\publish-win -e CoreMDFe.Desktop.exe

```

1. **Publicar (PowerShell):**

```powershell
$env:DOTNET_ROLL_FORWARD="Major"
vpk pack -u CoreMDFe -v 1.0.2 -p .\publish-win -e CoreMDFe.Desktop.exe

```

2. **Gerar Pacotes Velopack:**
```bash
vpk pack -u CoreMDFe -v 1.0.2 -p ./publish-win -e CoreMDFe.Desktop.exe

```

### Passo 3: Empacotar para LINUX

Abra o terminal de um ambiente Linux (ou WSL no Windows), navegue até a pasta `CoreMDFe.Desktop` e execute:

1. **Publicar (Publish Auto-contido):**
```bash
export DOTNET_ROLL_FORWARD=Major
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish-linux

```


2. **Gerar Pacotes Velopack:** *(Atenção: O executável no Linux não possui `.exe`)*
```bash
vpk pack -u CoreMDFe -v 1.0.2 -p ./publish-linux -e CoreMDFe.Desktop

```

---
## 🐧 Aviso para Usuários Linux (Executando o AppImage)

O CoreMDFe é distribuído no Linux através de um formato universal (`.AppImage`). Dependendo da sua distribuição, você pode precisar instalar a biblioteca **FUSE** para permitir a montagem e execução do aplicativo:

* **Ubuntu 22.04+, Pop!_OS, Mint e derivados recentes:**
```bash
sudo apt install libfuse2

```

* **Debian 12 e derivados:**
```bash
sudo apt install libfuse2t64

```

Após instalar a dependência, basta dar permissão de execução ao arquivo (`chmod +x CoreMDFe-*.AppImage`) e dar um duplo clique para iniciar o sistema!

---

## 🚀 Publicando as Atualizações (GitHub Releases)

O sistema de auto-update do CoreMDFe lê diretamente as *Releases* do GitHub.

1. Acesse a aba **Releases** deste repositório e clique em **Draft a new release**.
2. Em **Tag**, informe a versão exata que você compilou (ex: `1.0.2`).
3. Adicione um título e um *changelog* com as novidades.
4. Na área de anexos (*Attach binaries*), arraste **TODOS** os arquivos gerados nas pastas `Releases` do seu computador (Tanto os do Windows quanto os do Linux):
* `Setup.exe` (Instalador Windows)
* `CoreMDFe-1.0.2-linux-x64.AppImage` (Instalador/Portable Linux)
* `CoreMDFe-1.0.2-win-x64-Portable.zip` (Portable Windows)
* `CoreMDFe-1.0.2-win-x64-full.nupkg` e `CoreMDFe-1.0.2-linux-x64-full.nupkg` (Pacotes de Atualização)
* Arquivos `RELEASES` e `RELEASES-linux` (Índices do Velopack)


5. Clique em **Publish release**.

Pronto! Os clientes que já possuem o sistema verão o aviso de nova versão e o app fará o download da atualização de forma incremental e automática.

---

**Desenvolvido com ❤️ e rigor técnico para entregar a melhor experiência na emissão de MDF-e.**