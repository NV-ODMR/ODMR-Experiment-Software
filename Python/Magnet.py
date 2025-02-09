import scipy.special as ellip
import scipy.integrate as inte
import scipy.optimize as opt
import numpy


def GetE(k):
    return ellip.ellipe(k)


# 第二类椭圆积分
def GetK(k):
    return ellip.ellipk(k)


def Getrou2(x, y):
    return x * x + y * y


def Getr2(x, y, z, h):
    return x * x + y * y + (z - h) * (z - h)


def Getalpha2(r0, x, y, z, h):
    return r0 ** 2 + Getr2(x, y, z, h) - 2 * r0 * numpy.sqrt(Getrou2(x, y))


def Getbeta2(r0, x, y, z, h):
    return Getalpha2(r0, x, y, z, h) + 4 * r0 * numpy.sqrt(Getrou2(x, y))


def Getk2(r0, x, y, z, h):
    return 1 - Getalpha2(r0, x, y, z, h) / Getbeta2(r0, x, y, z, h)


def getiG(r0, l0, x, y, z, h):
    alpha2 = Getalpha2(r0, x, y, z, h)
    beta2 = Getbeta2(r0, x, y, z, h)
    rou2 = Getrou2(x, y)
    r2 = Getr2(x, y, z, h)
    e = GetE(Getk2(r0, x, y, z, h))
    k = GetK(Getk2(r0, x, y, z, h))
    return ((r0 ** 2 + r2) * e - alpha2 * k) * (z - h) / (2 * alpha2 * numpy.sqrt(beta2) * rou2)


def getiBz(r0, l0, x, y, z, h):
    alpha2 = Getalpha2(r0, x, y, z, h)
    beta2 = Getbeta2(r0, x, y, z, h)
    rou2 = Getrou2(x, y)
    r2 = Getr2(x, y, z, h)
    e = GetE(Getk2(r0, x, y, z, h))
    k = GetK(Getk2(r0, x, y, z, h))
    return ((r0 ** 2 - r2) * e + alpha2 * k) / (2 * alpha2 * numpy.sqrt(beta2))


def GetG(r0, l0, x, y, z):
    return inte.quad(lambda h: getiG(r0, l0, x, y, z, h), -l0 / 2, l0 / 2)[0]


def GetGZ(r0, l0, x,y, z):
    return inte.quad(lambda h: getiBz(r0, l0, x, y, z, h), -l0 / 2, l0 / 2)[0]
