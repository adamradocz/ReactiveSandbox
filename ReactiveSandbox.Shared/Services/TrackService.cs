using DynamicData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveSandbox.Shared.Models;
using ReactiveSandbox.Shared.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ReactiveSandbox.Shared.Services;

public class TrackService : IDisposable
{
    private readonly IOptions<AppOption> _options;
    private readonly SourceCache<TrackViewModel, int> _tracks = new(track => track.Id);
    private readonly CompositeDisposable _cleanup = new();
    private bool _disposedValue;

    public TrackService(GeneratorService generatorService, IOptions<AppOption> options, ILoggerFactory loggerFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Add or update tracks
        _ = generatorService.Tracks.Subscribe(trackDtos => _tracks.Edit(innerTracks =>
            {
                foreach (var trackDto in trackDtos)
                {
                    if (!IsTrackValid(trackDto))
                    {
                        continue;
                    }

                    var track = innerTracks.Lookup(trackDto.Id);
                    if (track.HasValue)
                    {
                        track.Value.Update(trackDto);

                        //You may also need to do the following [if properties are used for filtering, sorts, grouping etc]
                        innerTracks.Refresh(track.Value.Id);
                    }
                    else
                    {
                        innerTracks.AddOrUpdate(new TrackViewModel(trackDto, options, loggerFactory.CreateLogger<TrackViewModel>()));
                    }
                }
            }))
            .DisposeWith(_cleanup);

        // Remove expired tracks
        _ = _tracks.Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .WhenPropertyChanged(track => track.State)
            .Subscribe(track =>
            {
                if (track.Value == State.Expired)
                {
                    _tracks.RemoveKey(track.Sender.Id);
                }
            })
            .DisposeWith(_cleanup);
    }

    public IObservable<IChangeSet<TrackViewModel, int>> Connect() => _tracks.Connect();

    public int ClearTracks()
    {
        var count = _tracks.Count;
        _tracks.Clear();
        return count;
    }

    private bool IsTrackValid(in TrackDto track)
    {
        var futureTimeTolerance = DateTime.Now + _options.Value.FutureToleranceTime;
        var expiredToleranceTime = DateTime.Now - _options.Value.ExpiredToleranceTime;

        return track.Time < futureTimeTolerance && track.Time > expiredToleranceTime;
    }

    #region IDisposable
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _tracks.Dispose();
                _cleanup.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
