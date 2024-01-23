local a, b, c = 1, 2, 3, 4
local e, f = 1

b = a + c
a = b + c
c = a + b

e, f = a + b, b + c

do
    local d = a + b + c
    print(d)
end

print(a, b, c, d, e, f)

--[[
    Global Scope: 0xBE074 (Variables: [0xF96992, 0x1A6DD4A], Scopes: [0x2B02715])
    0x2B02715 (Variables: [0x7334C6, 0x1735440, 0x275E954, 0x3AC23EB, 0x11EF252], Scopes: [0x389E0CB]) {
        a (0x7334C6) = 1;
        b (0x1735440) = 2;
        c (0x275E954) = 3;
        4;
        e (0x3AC23EB) = 1;
        f (0x11EF252) = nil;
        b (0x1735440) = a (0x7334C6) + c (0x275E954);
        a (0x7334C6) = b (0x1735440) + c (0x275E954);
        c (0x275E954) = a (0x7334C6) + b (0x1735440);
        e (0x3AC23EB) = a (0x7334C6) + b (0x1735440);
        f (0x11EF252) = b (0x1735440) + c (0x275E954);
        0x389E0CB (Variables: [0x24F70AF], Scopes: []) {
            d (0x24F70AF) = a (0x7334C6) + b (0x1735440) + c (0x275E954);
            print (0xF96992)(d (0x24F70AF));
        }
        print (0xF96992)(a (0x7334C6), b (0x1735440), c (0x275E954), d (0x1A6DD4A), e (0x3AC23EB), f (0x11EF252));
    }
]]