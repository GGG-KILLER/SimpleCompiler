local a, b, c = 1, 2, 3, 4

b = a + c
a = b + c
c = a + b

do
    local d = a + b + c
    print(d)
end

print(a, b, c)