using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Chip8.Core;

using System.IO;

public class Chip8
{
    // memory
    private byte[] memory = new byte[4096];
    
    // registers
    private byte[] v = new byte[16];
    
    // program counter
    private ushort index;
    private ushort pc;
    
    // current opcode
    // one opcode is two bytes long.
    private ushort opcode;
    
    // stack 
    //private ushort[] stack = new ushort[16];
    private Stack<ushort> stack = new Stack<ushort>(16);
    private ushort stackPointer;
    
    // display
    // top-left is 0x00, bottom-right is 0x3F, 0x1F
    public bool[,] display =  new bool[64, 32];
    
    // timer
    public byte delayTimer = 0;
    public byte soundTimer = 0;
    
    // input
    public bool[] key = new bool[16];
    
    private byte[] fontset = [
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        ];
    
    
    
    // first order of business; parse Chip-8 bytecode.

    public Chip8(String rom)
    {
        LoadRom(rom);
        InitializeChip8();
    }

    public void LoadRom(String rom)
    {
        using (var stream = TitleContainer.OpenStream(rom))
        {
            Console.WriteLine("length: " + stream.Length);
            // read in each byte into memory, starting at offset 0x200
            // (before then is reserved for system data)
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                var array = ms.ToArray();

                for (int i = 0; i < array.Length; i++)
                {
                    memory[i + 0x200] = array[i];
                    Console.WriteLine(memory[i + 0x200]);

                }
            }
        }    
    }

    private void InitializeChip8()
    {
        for (int i = 0; i < key.Length; i++)
        {
            key[i] = false;
        }
        
        pc = 0x200;
        opcode = 0;
        index = 0;
        stackPointer = 0;

        //todo; clear display, stack, registers V0-VF, memory
        for (int i = 0; i < v.Length; i++)
        {
            v[i] = 0;
        }
        
        //todo; load fontset
        for (int i = 0; i < fontset.Length; i++)
        {
            memory[i + 0x050] = fontset[i];
        }
    }

    public void EmulateCycle()
    {
        // fetch opcode; read first 4 bits of opcode to find out what the opcode is, and what needs to bedone.
        opcode = (ushort)(memory[pc] << 8 | memory[pc + 1]);

        // decode opcode && execute opcode 
        // todo; 00E0, 1NNN, 6XNN, 7XNN, ANNN, DXYN will get implemented first.
        // this will allow us to run ibm.ch8, and from there other test suites / programs
        switch (opcode & 0xF000)
        {
            case 0x0000:
                switch (opcode & 0x000F)
                {
                    case 0x0000: // 0x00E0: clears the screen
                        display = new bool[64, 32];
                        pc += 2;
                        break;
                    case 0x000E: // 0x00EE: returns from subroutine
                        var returnAddress = stack.Pop();
                        pc = returnAddress;
                        pc += 2; // this feels wrong
                        Debug.WriteLine("Popping stack, 00EE");
                        break;
                    default:
                        Console.WriteLine("unknown opcode [0x0000]" + opcode);
                        break;
                }
                break;
            
            case 0x1000:
                //jump command; don't set pc here.
                pc = (ushort)(opcode & 0x0FFF);
                break;
            
            case 0x2000:
                // NNN (call a subroutine)
                // Push current PC to the stack, and then set PC to NNN.
                stack.Push(pc);
                pc = (ushort)(opcode & 0x0FFF);
                Debug.WriteLine("Setting pc: " + pc);
                break;
            
            case 0x3000:
                // todo: if vx != NN then
                if (v[(ushort)((opcode & 0x0F00) >> 8)] == (byte)((opcode & 0x00FF)))
                {
                    pc += 4;
                }
                else
                {
                    pc += 2;
                }
                break;
            
            case 0x4000:
                //if vx == NN then
                if (v[(ushort)((opcode & 0x0F00) >> 8)] != (byte)((opcode & 0x00FF)))
                {
                    pc += 4;
                }
                else
                {
                    pc += 2;
                }
                break;
            
            case 0x5000:
                //if vx != vy then
                if (v[(ushort)((opcode & 0x0F00) >> 8)] == v[(ushort)((opcode & 0x00F0) >> 4)])
                {
                    pc += 4;
                }
                else
                {
                    pc += 2;
                }
                break;
            
            case 0x6000:
                //vx := NN
                v[(ushort)((opcode & 0x0F00) >> 8)] = (byte)((opcode & 0x00FF));
                pc += 2;
                break;
            
            case 0x7000:
                //vy += NN
                v[(ushort)((opcode & 0x0F00) >> 8)] += (byte)((opcode & 0x00FF));
                pc += 2;
                break;
            
            case 0x8000:
                switch (opcode & 0x000F)
                {
                    case 0x0000:
                        //vx := vy
                        v[(ushort)((opcode & 0x0F00) >> 8)] = v[(ushort)((opcode & 0x00F0) >> 4)];
                        pc += 2;
                        break;
                    case 0x0001:
                        //vx |= vy
                        v[(ushort)((opcode & 0x0F00) >> 8)] |= v[(ushort)((opcode & 0x00F0) >> 4)];
                        pc += 2;
                        break;
                    case 0x0002:
                        //vx &= vy
                        v[(ushort)((opcode & 0x0F00) >> 8)] &= v[(ushort)((opcode & 0x00F0) >> 4)];
                        pc += 2;
                        break;
                    case 0x0003:
                        //vx ^= vy
                        v[(ushort)((opcode & 0x0F00) >> 8)] ^= v[(ushort)((opcode & 0x00F0) >> 4)];
                        pc += 2;
                        break;
                    case 0x0004:
                        //vx += vy. vf = 1 on carry.
                        v[(ushort)((opcode & 0x0F00) >> 8)] += v[(ushort)((opcode & 0x00F0) >> 4)];
                        pc += 2;
                        break;
                    case 0x0005:
                        //vx -= vy. vf = 0 on borrow.
                        v[(ushort)((opcode & 0x0F00) >> 8)] -= v[(ushort)((opcode & 0x00F0) >> 4)];
                        pc += 2;
                        break;
                    case 0x0006:
                        //vx >>= vy. vf = old least significant bit. currently fails opcode tests.
                        v[(ushort)((opcode & 0x0F00) >> 8)] = (byte)(v[(ushort)((opcode & 0x00F0) >> 4)] >> 1);
                        pc += 2;
                        break;
                    case 0x0007:
                        //vx =- vy. vf = 0 on borrow.
                        v[(ushort)((opcode & 0x0F00) >> 8)] = (byte)(v[(ushort)((opcode & 0x00F0) >> 4)] - v[(ushort)((opcode & 0x0F00) >> 8)]);
                        pc += 2;
                        break;
                    case 0x000E:
                        //vx <<= vy. vf = old most significant bit. currently fails opcode tests.
                        v[(ushort)((opcode & 0x0F00) >> 8)] = (byte)(v[(ushort)((opcode & 0x00F0) >> 4)] << 1);
                        pc += 2;
                        break;
                    default:
                        Console.WriteLine("unknown opcode [0x8000]" + opcode);
                        break;
                }
                break;
            
            case 0x9000:
                //if vx == vy then
                if (v[(ushort)((opcode & 0x0F00) >> 8)] != v[(ushort)((opcode & 0x00F0) >> 4)])
                {
                    pc += 4;
                }
                else
                {
                    pc += 2;
                }
                break;
            
            case 0xA000:
                //i := NNN
                index = (ushort)(opcode & 0x0FFF);
                pc += 2;
                break;
            
            case 0xB000:
                //jump0 NNN (jump to address NNN + v[0])
                pc = (ushort)((opcode & 0x0FFF) + v[0]);
                break;
            
            case 0xC000:
                //todo: vx := random NN (random number 0-255 and NN)
                Random rand = new Random();
                var randomNumber = rand.Next(0, 255);

                v[(opcode & 0x0F00) >> 8] = (byte)(randomNumber & (opcode & 0x00FF));
                
                break;
            
            case 0xD000:
                // display/draw function. also technically handles collision checking. 
                ushort x = (ushort)(v[(opcode & 0x0F00) >> 8] & 63); // set X coord to the value in VX modulo 64 (or VX & 63)
                ushort y = (ushort)(v[(opcode & 0x00F0) >> 4] & 31); // set Y coord to the value in VY modulo 32 (or VX & 31)
                ushort height = (ushort)(opcode & 0x000F); // get the height
                
                v[0xF] = 0; // set VF to 0
                
                // for n rows
                for (int yLine = 0; yLine < height; yLine++)
                {
                    // get Nth byte of sprite data, counting from memory address in the I register (dont increment I)
                    // pixel = Convert.ToBoolean(memory[index + yLine]); // 0 is false, anything else is true.
                    int pixel = memory[index + yLine];
                    
                    // for each of the 8 pixels / bits in this sprite row
                    for (int xLine = 0; xLine < 8; xLine++)
                    {
                        if ((pixel & (0x80 >> xLine)) != 0) // scan through the byte, one bit at a time
                        {
                            if (display[x + xLine, y + yLine])
                            {
                                v[0xF] = 1;
                                display[x + xLine, y + yLine] = false;
                            } else if (!display[x + xLine, y + yLine])
                            {
                                // if the current pixel in the sprite is on but the pixel at X, Y isn't, draw the pixel at X, Y
                                display[x + xLine, y + yLine] = true;
                            }
                        }
                        // todo; if you reach the right edge of the screen (past 64), stop drawing this row.
                    }
                    // todo; stop drawing if you reach the bottom edge of the screen
                }
                pc += 2;
                break;
            
            case 0xE000:
                switch (opcode & 0x00FF)
                {
                    case 0x009E:
                        // todo: if vx -key then (is key not pressed)
                        if (key[v[(opcode & 0x0F00) >> 8]] == false)
                        {
                            pc += 4;
                        }
                        else
                        {
                            pc += 2;
                        }
                        break;
                    case 0x00A1:
                        // todo; if vx key then (is key pressed)
                        if (key[v[(opcode & 0x0F00) >> 8]] == true)
                        {
                            pc += 4;
                        }
                        else
                        {
                            pc += 2;
                        }
                        break;
                }
                break;
            
            case 0xF000:
                switch (opcode & 0x00FF)
                {
                    case 0x0007:
                        // todo: vx := delay.
                        v[(opcode & 0x0F00) >> 8] = delayTimer;
                        pc += 2;
                        break;
                    case 0x000A:
                        // todo: vx := key (wait for keypress)
                        if (key[v[(opcode & 0x0F00) >> 8]] == true)
                        {
                            pc += 2;
                        }
                        else
                        {
                            pc -= 2;
                        }
                        break;
                    case 0x0015:
                        // todo; delay := vx;
                        delayTimer = v[(opcode & 0x0F00) >> 8];
                        pc += 2;
                        break;
                    case 0x0018:
                        // todo; buzzer := vx
                        soundTimer = v[(opcode & 0x0F00) >> 8];
                        pc += 2;
                        break;
                    case 0x001E:
                        // todo; i += vx.
                        // the amiga interpreter sets VF to 1 if index overflows. 
                        // one piece of software (Spacefight 2091!) relies on this behaviour.
                        // doesn't seem like other software relies on it though. 
                        index += (ushort)(v[(opcode & 0x0F00) >> 8]);
                        pc += 2;
                        break;
                    case 0x0029:
                        // todo; i := hex vx (set i to a hex character)
                        pc += 2;
                        break;
                    case 0x0033:
                        // todo; bcd vx (decode vx into binary-coded decimal). currently fails opcode tests.
                        var op = ((opcode & 0x0F00) >> 8);
                        var value = v[op];

                        var one = value / 100;
                        var two = (value - one * 100) / 10;
                        var three = value - one * 100 - two * 10;

                        memory[index] = (byte)one;
                        memory[index + 1] = (byte)two;
                        memory[index + 2] = (byte)three;
                        
                        pc += 2;
                        break;
                    case 0x0055: 
                        // todo; save vx (save v0-vx to i through (i + x). currently fails opcode test. 
                        // ambiguous instruction. early interpreters incremented i, and the value would change after.
                        // modern interpreters don't do this.
                        // might wanna add a switch for compatibility later.
                        
                        for (int i = 0; i < ((opcode & 0x0F00) >> 8 + 1); ++i)
                        {
                            memory[index + i] = v[i];
                        }
                        
                        index = (ushort)(((opcode & 0x0F00) >> 8) + 1);
                        pc += 2;
                        break;
                    case 0x0065:
                        // todo; load vx (load v0-vx from i through (i + x). currently fails opcode test.
                        // ambiguous instruction. early interpreters incremented i, and the value would change after.
                        // modern interpreters don't do this.
                        // might wanna add a switch for compatibility later.
                        for (int i = 0; i < (opcode & 0x0F00) >> 8; i++)
                        {
                            v[i] = memory[index + i];
                        }

                        pc += 2;
                        break;
                }
                break;
            
            default:
                Console.WriteLine("unknown opcode" + opcode);
                break;
        }

        // update timers
        // todo; add timers
        
    }
}