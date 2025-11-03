# Novas Funcionalidades – MauiPets

<img width="388" height="800" alt="Update_10_2025" src="https://github.com/user-attachments/assets/0a97ebb2-073a-40a1-a790-0d52840691b4" />
---

## 📸 Galeria de Fotos do Pet

- **Gestão de Galeria por Animal**
  - Cada pet tem agora uma galeria de fotos associada.
  - É possível:
    - Adicionar fotos (usando a câmara ou galeria do dispositivo).
    - Visualizar fotos em modo de galeria.
    - Eliminar fotos individuais.
    - Ampliar/visualizar foto em popup.
  - As fotos são guardadas localmente na app e associadas ao animal.

- **Integração UI/UX**
  - Acesso à galeria diretamente a partir do perfil do animal.
  - Confirmação visual e mensagens toast para feedback de ações (ex: remoção de foto).
  - Fácil navegação entre galeria e detalhes do pet.

---

### 📢 Notificações

**Objetivo:**  
Esta funcionalidade alerta o utilizador para a existência de notificações na aplicação que ainda não foram assinaladas como lidas ou tratadas.  
Destina-se a mensagens do sistema, lembretes de eventos, tarefas pendentes ou qualquer alerta que requeira atenção do utilizador.

**O que aparece na página:**  
- Um ícone de sino é apresentado no canto superior direito da página principal.
- Quando existem notificações ainda não assinaladas como lidas, surge um badge vermelho sobre o sino, mostrando o número de notificações pendentes.
- Ao tocar no sino, o utilizador acede à lista de notificações.
- Cada notificação pode ser marcada individualmente como lida/tratada pelo utilizador, desaparecendo assim do contador do badge.
- Apenas as notificações ainda não assinaladas como lidas (não tratadas) são contabilizadas e apresentadas; notificações já lidas não são consideradas.

**Tipos de notificações abrangidas:**  
- Lembretes de eventos ou compromissos.
- Alertas de tarefas pendentes.
- Mensagens de aviso do sistema.
- Outras comunicações internas que necessitem de ação do utilizador.

---

### 📄 Gestão de Documentos

A funcionalidade de Gestão de Documentos permite ao usuário fazer upload, visualizar, editar e excluir arquivos — como registros de vacinação, certificados ou qualquer documento relevante para um pet — vinculados a cada animal.

## ✨ Funcionalidades

- ➕ **Adicionar Documento:**  
  Faça upload de arquivos PDF usando o seletor, insira um título e descrição, e associe cada documento a um pet específico.

- 👀 **Visualizar Documentos:**  
  Veja todos os documentos de um pet, incluindo título, descrição, localização do arquivo, data de criação e nome do animal associado.

- ✏️ **Editar Documento:**  
  Altere o título, a descrição ou substitua o arquivo do documento.

- 🗑️ **Excluir Documento:**  
  Remova documentos do banco de dados e do armazenamento local; inclui confirmação antes de apagar.

- 📂 **Abrir Documentos:**  
  Abra arquivos usando o caminho associado com o visualizador de arquivos do sistema.

## 🚦 Fluxo de Uso

1. **Adicionar Documento:**  
   - No perfil do pet, selecione “Adicionar Documento” ➕.
   - Escolha um arquivo PDF 📄.
   - Insira Título e Descrição 📝.
   - Salve para vincular o documento ao animal 🐾.

2. **Editar ou Remover Documento:**  
   - Selecione a entrada desejada 📃.
   - Edite seus detalhes ✏️ ou clique em excluir 🗑️, com confirmação.

3. **Abrir/Visualizar Documento:**  
   - Toque no documento para abrir no visualizador padrão do sistema 📂.

## 💡 Observações

- Apenas arquivos PDF são suportados para upload.
- Os arquivos são salvos localmente no diretório de dados do app.
- O sistema garante nomes únicos para evitar conflitos.
- Ao excluir um documento, o armazenamento local também é limpo caso o arquivo exista.

---

# 📄 Gestão de Documentos - MauiPetsApp

A funcionalidade de Gestão de Documentos permite ao usuário fazer upload, visualizar, editar e excluir arquivos — como registros de vacinação, certificados ou qualquer documento relevante para um pet — vinculados a cada animal.

## ✨ Funcionalidades

- ➕ **Adicionar Documento:**  
  Faça upload de arquivos PDF usando o seletor, insira um título e descrição, e associe cada documento a um pet específico.

- 👀 **Visualizar Documentos:**  
  Veja todos os documentos de um pet, incluindo título, descrição, localização do arquivo, data de criação e nome do animal associado.

- ✏️ **Editar Documento:**  
  Altere o título, a descrição ou substitua o arquivo do documento.

- 🗑️ **Excluir Documento:**  
  Remova documentos do banco de dados e do armazenamento local; inclui confirmação antes de apagar.

- 📂 **Abrir Documentos:**  
  Abra arquivos usando o caminho associado com o visualizador de arquivos do sistema.

---

## 🌐 Opção de Idioma

Agora, na área de Configuração/Settings da aplicação, é possível escolher entre dois idiomas:

- 🇵🇹 **Português**
- 🇬🇧 **Inglês**

O usuário pode acessar a opção de idioma nas Configurações e alternar facilmente entre Português e Inglês.  
A escolha é aplicada instantaneamente em toda a interface do app, proporcionando uma experiência personalizada para diferentes perfis de utilizador.

**Como funciona:**  
- O idioma selecionado é guardado nas preferências do utilizador.
- A interface e todos os textos da aplicação são apresentados no idioma escolhido.
- Idiomas disponíveis: `Português (pt-PT)` e `English (en-US)`.

**Alterar Idioma:**  
   - Abra o menu Configuração 🌐.
   - Selecione entre Português 🇵🇹 e Inglês 🇬🇧 na opção de Idioma.
   - Pronto! A interface é atualizada para o idioma selecionado.

## 💡 Observações
- A troca de idioma é aplicada instantaneamente e é memorizada para futuras utilizações.

---
## 🔐 Backup e Restauração de Dados 

- **Backup Manual**
  - Possibilidade de criar backups da base de dados local da aplicação via interface.
  - O utilizador pode visualizar o nome, data e localização do último backup.
  - O backup é guardado em ficheiro local, com indicação visual de sucesso/erro.
  - Proteção contra sobreposição não-intencional: confirmação antes de substituir backups existentes.

- **Restauração Segura**
  - Permite restaurar a base de dados local a partir de um backup existente.
  - Confirmação obrigatória antes de substituir os dados atuais.
  - Informação visual sobre alterações entre o estado corrente e o backup.
  - Processo de restore com feedback ao utilizador e indicação de sucesso ou falha.

---

## 📄 Exportação e Partilha de Ficha PDF 

- **Geração de PDF com Ficha Completa do Animal**
  - Criação de um ficheiro PDF detalhado para cada pet, contendo:
    - Dados principais (nome, espécie, raça, idade, chip, etc.)
    - Historial de vacinas, desparasitantes, rações e consultas veterinárias.
    - Historial de consultas (Veterinário, ...).
  
  

- **Partilha Simplificada**
  - O PDF pode ser partilhado diretamente através das opções nativas do dispositivo (e-mail, WhatsApp, etc.).

---

## Segurança e Privacidade

- **Validação e Confirmação em Ações Críticas**
  - Ações de backup/restauração e eliminação de fotos requerem confirmação do utilizador.
  - Mensagens claras e feedback visual em todas as operações sensíveis.

- **Gestão Local dos Dados**
  - Fotos e ficheiros de backup são geridos localmente, respeitando a privacidade do utilizador.
  - Não há envio de dados sensíveis para servidores externos sem ação do utilizador.

---

*Para mais detalhes sobre cada funcionalidade, explore a interface da aplicação.*
