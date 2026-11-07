namespace MotionFramework
{
	public interface IProgress
	{
		/// <summary>
		/// 轮询模块
		/// </summary>
		float GetProgress();

		bool IsDone();
	}
}