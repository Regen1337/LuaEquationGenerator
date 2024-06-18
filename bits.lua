local RUN_TESTS = false
local REPLACE_BIT_LIB = true

local bits = _G.bits or bit or {}
local TWO_32 = 2^32

-- Bitwise NOT
function bits.bnot(x)
    x = x % TWO_32;
    return (TWO_32 - 1) - x;
end

-- Bitwise AND
function bits.band(x, y)
    local result = 0
    local bitval = 1
    while x > 0 and y > 0 do
        local rx, ry = x % 2, y % 2
        if rx == 1 and ry == 1 then
            result = result + bitval
        end
        bitval = bitval * 2
        x = math.floor(x / 2)
        y = math.floor(y / 2)
    end
    return result
end

-- Bitwise OR
function bits.bor(x, y)
    local result = 0
    local bitval = 1
    while x > 0 or y > 0 do
        local rx, ry = x % 2, y % 2
        if rx == 1 or ry == 1 then
            result = result + bitval
        end
        bitval = bitval * 2
        x = math.floor(x / 2)
        y = math.floor(y / 2)
    end
    return result
end

-- Bitwise XOR
function bits.bxor(x, y)
    local result = 0
    local bitval = 1
    while x > 0 or y > 0 do
        local rx, ry = x % 2, y % 2
        if rx ~= ry then
            result = result + bitval
        end
        bitval = bitval * 2
        x = math.floor(x / 2)
        y = math.floor(y / 2)
    end
    return result
end

-- Left shift
function bits.lshift(x, shift)
    return (x * 2^shift) % TWO_32
end

-- Logical right shift
function bits.rshift(x, shift)
    return math.floor(x / 2^shift)
end

-- Arithmetic right shift
function bits.arshift(x, shift)
    x = x % TWO_32

    if shift >= 0 then
        if shift > 31 then
            return (x >= 0x80000000) and (TWO_32 - 1) or 0
        else
            local z = math.floor(x / (2 ^ shift))
            if x >= 0x80000000 then
                z = z + (2 ^ shift - 1) * (2 ^ (32 - shift))
            end
            return z
        end
    else
        return math.floor(x * (2 ^ -shift))
    end
end

-- Rotate left shift
function bits.rol(x, shift)
    shift = shift % 32
    return bits.bor(bits.lshift(x, shift), bits.rshift(x, 32 - shift))
end

-- Rotate right shift
function bits.ror(x, shift)
    shift = shift % 32
    return bits.bor(bits.rshift(x, shift), bits.lshift(x, 32 - shift))
end

-- Bit test
function bits.btest(x, y) -- iffy on this one
    return bits.band(x, y) ~= 0
end

-- Extract bits
function bits.extract(x, field, width)
    local mask = bits.lshift(bits.lshift(1, width) - 1, field)
    return bits.rshift(bits.band(x, mask), field)
end

-- Replace bits
function bits.replace(x, v, field, width)
    local mask = bits.lshift(bits.lshift(1, width) - 1, field)
    v = bits.lshift(bits.band(v, bits.lshift(1, width) - 1), field)
    return bits.band(x, bits.bnot(mask)) + bits.band(v, mask)
end

-- Convert bits -> hex -> number
function bits.tohex(x, n) -- iffy on this one
    local hex_digits = "0123456789abcdef"
    local result = ""
    
    if n == nil then
        n = 8
    end
    
    for i = n - 1, 0, -1 do
        local digit = bits.band(bits.rshift(x, i * 4), 0xf)
        result = result .. string.sub(hex_digits, digit + 1, digit + 1)
    end
    
    return tonumber(result, 16)
end

if RUN_TESTS then
    -- Test bits.bnot
    assert(bits.bnot(0) == 0xFFFFFFFF)
    assert(bits.bnot(0xFFFFFFFF) == 0)
    assert(bits.bnot(0x12345678) == 0xEDCBA987)

    -- Test bits.band
    assert(bits.band(0x12345678, 0xFF00FF00) == 0x12005600)
    assert(bits.band(0x12345678, 0x0000FFFF) == 0x00005678)

    -- Test bits.bor
    assert(bits.bor(0x12345678, 0xFF00FF00) == 0xFF34FF78)
    assert(bits.bor(0x12345678, 0x0000FFFF) == 0x1234FFFF)

    -- Test bits.bxor
    assert(bits.bxor(0x12345678, 0xFF00FF00) == 0xED34A978)
    assert(bits.bxor(0x12345678, 0x0000FFFF) == 0x1234A987)

    -- Test bits.lshift
    assert(bits.lshift(0x12345678, 4) == 0x23456780)
    assert(bits.lshift(0x12345678, 8) == 0x34567800)

    -- Test bits.rshift
    assert(bits.rshift(0x12345678, 4) == 0x01234567)
    assert(bits.rshift(0x12345678, 8) == 0x00123456)

    -- Test bits.arshift
    assert(bits.arshift(0x12345678, 4) == 0x01234567)
    assert(bits.arshift(0xF2345678, 4) == 0xFF234567)

    -- Test bits.rol
    assert(bits.rol(0x12345678, 4) == 0x23456781)
    assert(bits.rol(0x12345678, 8) == 0x34567812)

    -- Test bits.ror
    assert(bits.ror(0x12345678, 4) == 0x81234567)
    assert(bits.ror(0x12345678, 8) == 0x78123456)

    -- Test bits.btest
    assert(bits.btest(0x12345678, 0x00FF0000) == true)
    assert(bits.btest(0x12345678, 0xFF000000) == true) 

    -- Test bits.extract
    assert(bits.extract(0x12345678, 8, 8) == 0x56)
    assert(bits.extract(0x12345678, 16, 8) == 0x34)

    -- Test bits.replace
    assert(bits.replace(0x12345678, 0xAB, 8, 8) == 0x1234AB78)
    assert(bits.replace(0x12345678, 0xCD, 16, 8) == 0x12CD5678)

    -- Test bits.tohex
    assert(bits.tohex(0x12345678) == 0x12345678)
    assert(bits.tohex(0xABCDEF01, 8) == 0xABCDEF01)

    print("All tests passed!")
end

if REPLACE_BIT_LIB then
    bit = bits
else
    _G.bits = bits
end