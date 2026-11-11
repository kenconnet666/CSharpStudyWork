"""Modbus TCP 仿真设备（pymodbus）：端口 5020，从站地址 1，保持寄存器 0~99。

寄存器含义与学习站里内存中的模拟设备一致，所以同一套上位机代码可以直接对着它读：
  [0] 温度 ×10    [1] 压力    [2] 状态字（第 0 位=运行，第 2 位=离线）
  [3][4] 温度的 float（大端，高字在前）    [5] 一直递增的计数
其余寄存器可以读也可以写（比如往 [10] 写一个设定值，再读回来）。
"""
import asyncio
import random
import struct

from pymodbus.constants import ExcCodes
from pymodbus.server import ModbusTcpServer
from pymodbus.simulator import DataType, SimData, SimDevice

state = {"temperature": 55.0, "counter": 0}


async def simulate() -> None:
    """让温度不断变化，像真实设备里的传感器在刷新数据。"""
    while True:
        state["temperature"] += (60 - state["temperature"]) * 0.05 + random.uniform(-0.4, 0.4)
        state["counter"] = (state["counter"] + 1) % 65536
        await asyncio.sleep(0.5)


def live_registers() -> dict[int, int]:
    temperature = state["temperature"]
    high, low = struct.unpack(">HH", struct.pack(">f", temperature))
    return {
        0: round(temperature * 10),
        1: round(100 + (temperature - 50) * 0.4),
        2: 0b0001,
        3: high,
        4: low,
        5: state["counter"],
    }


async def action(function_code, start_address, address, count, current_registers, set_registers) -> ExcCodes | None:
    """每次读请求前，把实时值写进寄存器表（current_registers 是整张表，下标就是寄存器地址）。
    写请求不处理：交给默认的存取，所以往 [10] 写的值会一直保存着。"""
    if set_registers is None:
        for register, value in live_registers().items():
            current_registers[register] = value
    return None


async def main() -> None:
    device = SimDevice(
        id=1,
        simdata=[SimData(address=0, count=100, values=0, datatype=DataType.REGISTERS)],
        action=action,
    )
    server = ModbusTcpServer(device, address=("0.0.0.0", 5020))
    print("Modbus TCP 仿真设备已启动：端口 5020，从站地址 1", flush=True)
    await asyncio.gather(server.serve_forever(), simulate())


asyncio.run(main())
