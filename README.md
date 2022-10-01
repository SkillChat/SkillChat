### [Switch to English Readme](README.EN.md)
## 📖 О проекте 

![Logo](https://user-images.githubusercontent.com/61462657/171970177-ef4aa4e0-33ae-4a27-a791-c5adb68f53e5.svg)

SkillChat - это кроссплатформенный open-source мессенджер с возможностями общения в чате в реальном времени, отправки файлов, получения уведомлений и возможностью развертывания на личном сервере.

---
## 🚀 Начало работы
### Для пользователей
1. [Скачайте](https://github.com/SkillChat/SkillChat/releases/download/v0.2.0/SkillChat.Release.zip) последнюю версию  
2. Запустите файл SkillChat.Server.exe из папки Server
3. Запустите файл SkillChat.Client.exe из папки Client  
(можно запускать несколько клиентов)
### Для разработчиков
1. Скачайте следующие пакеты и дополнения
> [.NET 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

> [Avalonia](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaforVisualStudio)

> [SwitchStartupProject](https://marketplace.visualstudio.com/items?itemName=vs-publisher-141975.SwitchStartupProjectForVS2019)

2. Клонируйте репозиторий
```sh
git clone https://github.com/SkillChat/SkillChat
```
3. Запустите проект, выбрав в SwitchStartupProject вариант Server + Client

![image](https://user-images.githubusercontent.com/61462657/172032136-95d55f65-8451-4fce-b46c-ea0da859006f.png )

## 🪤Правила разработки
### 🧹 Оформление кода
- Комментировать новый код
- Использовать табуляцию для отступов
- Выносить классы в отдельные файлы

### 🗃 Работа с Git
#### Типы коммитов

| Тип      | Описание                                                        |
| -------- | --------------------------------------------------------------- |
| buil     | Сборка проекта или изменения внешних зависимостей               |
| ci       | Настройка CI и работа со скриптами                              |
| docs     | Обновление документации                                         |
| feat     | Добавление нового функционала                                   |
| fix      | Исправление ошибок                                              |
| perf     | Изменения направленные на улучшение производительности          |
| refactor | Правки кода без исправления ошибок или добавления новых функций |
| revert   | Откат на предыдущие коммиты                                     |
| style    | Правки по кодстайлу (табы, отступы, точки, запятые и т.д.)      |
| test     | Добавление тестов                                               |

- В название коммитов/веток не выносить номер задачи
- Пишем описание в повелительном наклонении (imperative mood)
- Не закачиваем описание коммита знаками препинания  
##### Пример ветки: ` fix/fix-typos-in-titles `
##### Пример коммита: ` docs: Обновил README.md `
---
## 📺 Демонстрация работы

<video src="https://user-images.githubusercontent.com/61462657/172044241-fa4d2d4b-a5cb-4d15-b46c-85a11fb16c96.mp4" ></video>

<video src="https://user-images.githubusercontent.com/61462657/172043463-dc75a8e2-df2a-45f4-b866-fe70389f05dd.mp4" ></video>

<video src="https://user-images.githubusercontent.com/61462657/172043473-2e6c4ff4-455d-4ecc-a2c6-3ff61cc7f70c.mp4" ></video>

---
## 🛠️ Инструменты и технологии
<div>
    <a href="https://docs.microsoft.com/ru-ru/dotnet/" target="_blank">
      <img src="https://user-images.githubusercontent.com/61462657/171970442-3c60c757-6df1-4d2f-8d20-200e1f2d4448.svg"  title="C#" alt="С#" width="40" height="40"/>&nbsp;
    </a>
    <a href="https://avaloniaui.net/" target="_blank">
  <img src="https://user-images.githubusercontent.com/61462657/171970443-06d06ff4-6830-49e7-8d64-df37a3f47205.svg" title="AvaloniaUi" alt="AvaloniaUi" width="40" height="40"/>&nbsp;
    </a>
      <a href="https://servicestack.net/" target="_blank">
  <img src="https://user-images.githubusercontent.com/61462657/171977777-19c0bffc-48ae-4731-a437-850fccab2bd0.png" title="ServiceStack" alt="ServiceStack" width="40" height="40"/>&nbsp;
    </a>
      <a href="https://ravendb.net/" target="_blank">
  <img src="https://user-images.githubusercontent.com/61462657/171979984-bbd27329-e2ee-4883-94b2-695f1935762a.png" title="RavenDB" alt="RavenDB" width="40" height="40"/>&nbsp;
    </a>
      <a href="https://github.com/SignalR/SignalR" target="_blank">
 <img src="https://user-images.githubusercontent.com/61462657/171978461-101570ee-f828-478d-b132-cb5601a9c0a9.png" title="SignalR" alt="SignalR" width="40" height="40"/>&nbsp;   
    </a>
      <a href="https://automapper.org/" target="_blank">
  <img src="https://user-images.githubusercontent.com/61462657/171980547-0b97aec8-7e04-49e1-b6b5-8905651249b3.png" title="AutoMapper" alt="AutoMapper" width="40" height="40"/>&nbsp;
    </a>
</div>

---
 ## Реализованные функции
 + [Отправка файлов](https://github.com/SkillChat/SkillChat/pull/46)  
 + [Редактирование сообщений](https://github.com/SkillChat/SkillChat/pull/61)  
 + [Выбор сообщений](https://github.com/SkillChat/SkillChat/pull/89)  
 + [Цитирование сообщений](https://github.com/SkillChat/SkillChat/pull/83)  
 + [Удаление выбранных сообщений](https://github.com/SkillChat/SkillChat/pull/95)  
 + [Очистка чата](https://github.com/SkillChat/SkillChat/pull/95)  
 + [Цветные ники для пользователей](https://github.com/SkillChat/SkillChat/pull/108)  

---
