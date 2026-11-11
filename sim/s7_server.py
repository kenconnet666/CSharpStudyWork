"""西门子 S7 仿真 PLC（python-snap7 的服务端）：端口 102，DB1，64 字节。

DB1 的布局与学习站「西门子 S7」一课一致：
  DBW0  温度 ×10（Int）     DBW2  压力（Int）
  DBD4  温度（Real）        DBD8  计数（DInt）
  DBB12 状态字节（第 0 位=运行，第 2 位=离线）
上位机可以读，也可以写（比如往 DBW14 写一个设定值，再读回来）。
"""
import random
import struct
import time

from snap7.server import Server
from snap7.type import SrvArea

db1 = bytearray(64)   # 直接改这个 bytearray，服务端读到的就是新值


def put(offset: int, fmt: str, value) -> None:
    data = struct.pack(fmt, value)  # 西门子是大端：fmt 都以 ">" 开头
    db1[offset:offset + len(data)] = data


server = Server()
server.register_area(SrvArea.DB, 1, db1)
server.start(tcp_port=102)
print("S7 仿真 PLC 已启动：端口 102，DB1（64 字节）", flush=True)

temperature = 55.0
counter = 0
put(12, ">B", 0b0001)
while True:
    temperature += (60 - temperature) * 0.05 + random.uniform(-0.4, 0.4)
    counter += 1
    put(0, ">h", round(temperature * 10))
    put(2, ">h", round(100 + (temperature - 50) * 0.4))
    put(4, ">f", temperature)
    put(8, ">i", counter)
    time.sleep(0.5)
