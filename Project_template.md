# Задание 1

[Контейнерная диаграмма C4 To Be](docs/to-be.drawio)

[Просмотр диаграммы](docs/to-be.svg)

# Задание 2

### 1. Proxy

[Сервис proxy на C#](src/microservices/proxy)

`/api/movies` переключается между монолитом и movies-service. `/api/events` направляется в events-service. Остальные запросы идут в монолит.

При `GRADUAL_MIGRATION=true` используется `MOVIES_MIGRATION_PERCENT`; при `false` все запросы movies направляются в новый сервис.

| Доля миграции | Монолит | Movies |
| --- | ---: | ---: |
| 0% | 30 | 0 |
| 50% | 20 | 10 |
| 100% | 0 | 30 |

[Проверка переключения](docs/checks/migration.txt)

### 2. Kafka

[Сервис events на C#](src/microservices/events)

События Movie, User и Payment записываются в отдельные топики и читаются consumer внутри сервиса. Обработка записывается в лог.

Postman: 22 запроса, 42 проверки, 0 ошибок.

[Результаты Postman](docs/checks/postman-local.txt)

[Состояние топиков](docs/checks/kafka-topics.txt)

Скриншоты Postman и Kafka UI пока не приложены.

# Задание 3

### CI/CD

[Сборка и публикация четырёх образов](.github/workflows/docker-build-push.yml)

[API-тесты текущего коммита](.github/workflows/api-tests.yml)

Запуск в GitHub Actions и публикация в GHCR ещё не выполнены.

### Proxy в Kubernetes

[Deployment и Service proxy](src/kubernetes/proxy-service.yaml)

[Deployment и Service events](src/kubernetes/events-service.yaml)

[Ingress](src/kubernetes/ingress.yaml)

[Состояние Pod](docs/checks/kubernetes-pods.txt)

[Ответ HTTPS API movies](docs/checks/kubernetes-movies-https.txt)

[Postman в Kubernetes](docs/checks/postman-kubernetes.txt)

[Логи обработки событий](docs/checks/kubernetes-events.txt)

Postman: 22 запроса, 42 проверки, 0 ошибок.

Скриншоты списка фильмов и логов пока не приложены.

# Задание 4

[Helm-чарт](src/kubernetes/helm)

[Values](src/kubernetes/helm/values.yaml)

[Шаблон proxy](src/kubernetes/helm/templates/services/proxy-service.yaml)

[Шаблон events](src/kubernetes/helm/templates/services/events-service.yaml)

Проверены чистая установка и обновление release. Helm lint: 0 ошибок. Все 7 Pod готовы. Postman: 22 запроса, 42 проверки, 0 ошибок.

[Установка](docs/checks/helm-install.txt)

[Статус release](docs/checks/helm-status.txt)

[Состояние Pod](docs/checks/helm-pods.txt)

[Ответ HTTPS API movies](docs/checks/helm-movies-https.txt)

[Postman после установки Helm](docs/checks/postman-helm.txt)

[Логи обработки событий](docs/checks/helm-events.txt)

Скриншоты установки и списка фильмов пока не приложены.
