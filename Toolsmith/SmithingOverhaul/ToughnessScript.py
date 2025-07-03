from scipy import integrate
import json
import math

with open("C:\\Users\\voupa\\AppData\\Roaming\\Vintagestory\\assets\\survival\\worldproperties\\block\\metal.json") as f:
    d = json.load(f)
    
#for metal in d["variants"]:
    #HardeningCoeff = math.log(metal["tensileStrength"] / metal["yieldStrength"]) / math.log(metal["elongation"] / 0.2)
    #StrengthCoeff = metal["yieldStrength"] / math.pow(0.2, HardeningCoeff)
    #ss = lambda stress: (stress / (metal["youngsModulus"] * 10)) + math.pow(stress / StrengthCoeff, 1 / HardeningCoeff)
    #upper, error = integrate.quad(ss, 0, metal["tensileStrength"])
    #toughness = metal["tensileStrength"] * metal["elongation"] - upper
    #print(metal["code"] + ": " + str(round(toughness)))

for metal in d["variants"]:
    crystalTemp = 0
    if (metal["elemental"]): crystalTemp =  0.35 * (metal["meltPoint"] + 273.15) - 273.15
    else: crystalTemp = 0.5 * (metal["meltPoint"] + 273.15) - 273.15
    WarmForgingTemp = 0.6 * (crystalTemp + 273.15) - 273.15
    print(metal["code"] + ": " + str(round(WarmForgingTemp)))