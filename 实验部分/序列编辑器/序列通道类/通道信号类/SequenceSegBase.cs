namespace ODMR_Lab.实验部分.序列编辑器
{
    /// <summary>
    /// 脉冲峰位置
    /// </summary>
    public abstract class SequenceSegBase
    {
        /// <summary>
        /// 脉冲名称
        /// </summary>
        public string PeakName { get; set; } = "";

        public SequenceChannelData ParentChannel { get; set; } = null;

        /// <summary>
        /// 起始脉冲时间
        /// </summary>
        public virtual int PeakSpan { get; set; } = 0;

        public abstract SequenceSegBase Copy();

        public abstract bool IsWaveOne();

        public abstract bool IsTrigger();

        public abstract void UpdateGlobalPulse(bool throwexception = false);
    }
}
