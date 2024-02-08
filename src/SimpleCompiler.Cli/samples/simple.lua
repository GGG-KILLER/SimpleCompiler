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