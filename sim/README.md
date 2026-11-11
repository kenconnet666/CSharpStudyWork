# 设备仿真（学习站「对接仿真设备」一课用）

在 WSL 的 Docker 里跑两台仿真设备，让上位机练真连接：

| 设备 | 地址 | 说明 |
|---|---|---|
| Modbus TCP（pymodbus） | `127.0.0.1:5020`，从站 1 | 保持寄存器 0~99；`[0]` 温度×10、`[1]` 压力、`[2]` 状态字、`[3][4]` 温度 float（大端）、`[5]` 计数；其余可读写 |
| 西门子 S7（python-snap7） | `127.0.0.1:102`，机架 0 槽 1 | DB1 共 64 字节；DBW0 温度×10、DBW2 压力、DBD4 Real、DBD8 DInt 计数、DBB12 状态；其余可读写 |

```bash
cd sim
docker compose up -d --build      # 启动（首次构建需要联网）
docker compose logs -f            # 看日志
docker compose down               # 停止
```

- 读数每 0.5 秒变化一次，温度在 60 ℃ 上下波动。
- 仿真只验证协议层；真设备还有延迟、丢包、权限设置（S7 要开 PUT/GET）等差别。
- 修改 `modbus_server.py` / `s7_server.py` 后重新 `docker compose up -d --build`。
