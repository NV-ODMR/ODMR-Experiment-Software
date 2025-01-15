# 调用扫描ＣＷ谱的Labview vi
import math

from labview_automation import LabVIEW

lv = LabVIEW()

CWFilePath = 'E:\\ODMR_labview\\磁场调平 2016版本\\CW_degen_ESEEM.vi'


# 运行CW谱
# 起始频率(MHz)
# 终止频率(MHz)
# 步长(MHz)
# 取样数
def RunCWGetRawData(startFreq: float, endFreq: float, step: float, averagetime: int, peakcount: int,
                    scanRangeAfterstop: int = 20, isReverse: bool = False):
    freqs = list()
    lv.start()
    if isReverse:
        direction = -1
    else:
        direction = 1
    with lv.client() as l:
        control_values = {'Start Frequency/MHz': startFreq,
                          'End Frequency/MHz': endFreq,
                          'Step(MHz)': step,
                          'Average times': averagetime,
                          '预计的谱峰数量': peakcount,
                          '停止之后继续扫描范围': scanRangeAfterstop,
                          '是否反向': direction}
        indicator = l.run_vi_synchronous(CWFilePath, control_values, open_frontpanel=True,
                                         indicator_names=['Fitted frequency(MHz)', 'Fitted frequency(MHz)2',
                                                          'Fitted contrast', 'Fitted contrast2',
                                                          "Ratio of averaged data"])
        f1 = float(indicator['Fitted frequency(MHz)'])
        f2 = float(indicator['Fitted frequency(MHz)2'])
        c1 = float(indicator['Fitted contrast'])
        c2 = float(indicator['Fitted contrast2'])
        c3 = indicator['Ratio of averaged data']
        freqs = c3[0]['Frequencies']
        counts = c3[0]['Binned Counts']
        lv.kill()
    return [f1, f2, c1, c2, freqs, counts]
