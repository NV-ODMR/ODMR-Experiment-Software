import scipy.special as ellip
import scipy.integrate as inte
import scipy.optimize as opt
import numpy

def GetE(k):
	return ellip.ellipe(k)

def GetK(k):
	return ellip.ellipk(k)

def AngleFunc(x, a, b, c):
    return a * numpy.cos((x - b) * numpy.pi / 180) + c

def FitSinData(locs, values):
   res, cov = opt.curve_fit(AngleFunc, locs, values, p0=[1, 180, 1],bounds=([-np.inf, 0, -np.inf], [np.inf, 360, np.inf]))
   return res
