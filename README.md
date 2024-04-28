# Momus (moʊməs)

Named after the Greek god of satire and mockery, also known for his sharp wit and critical eye, Momus scrutinizes and intelligently routes network traffic. By leveraging the dynamic nature of NATS and the robustness of YARP, Momus playfully mocks the complexities of service communication, offering a simplified, yet powerful, solution for modern application architectures.

## Overview

At its core, Momus uses NATS for disseminating routing configurations and YARP for handling incoming HTTP requests and routing them to the appropriate backend services. This synergy allows Momus to offer a seamless reverse proxy experience that is both dynamic and resilient.

The architecture of Momus is such that it subscribes to a NATS KeyValue store, which holds the routing configurations. Whenever a change is detected in the configuration, Momus updates its routes accordingly, without the need for service restarts. This offers a significant advantage in terms of uptime and flexibility.

![Momus Architecture](/docs/diagram.png)

## Features

- **Real-Time Configuration Updates**: Utilize NATS KeyValue stores to manage and apply routing configurations dynamically.
- **YARP Integration**: Leverage the advanced features of YARP to efficiently proxy HTTP requests to backend services.
- **Resilient and Scalable**: Designed to handle failures gracefully and scale with your application's needs.

## Demo Project

To see Momus in action, refer to the `ConfigUpdater` demo project included in the repository. This project illustrates how to publish updates to the routing configuration, showcasing the dynamic nature of Momus.

## License

Momus is released under the MIT License. See the LICENSE file for more details.

## Acknowledgments

This project leverages the power of [YARP](https://microsoft.github.io/reverse-proxy/) and [NATS](https://nats.io), and we are grateful to the maintainers of these projects for their contributions to the open-source community.