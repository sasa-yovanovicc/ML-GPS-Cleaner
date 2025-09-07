import React from 'react';
import { createRoot } from 'react-dom/client';
import { MapContainer, TileLayer, Polyline, CircleMarker, useMap } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import axios from 'axios';

interface RawPoint { id:number; deviceId:number; deviceTime:string; lat:number; lng:number; speedKph:number; }
interface CleanedPoint { id:number; deviceId:number; deviceTime:string; lat:number; lng:number; speedKph:number; isOutlier:boolean; isInterpolated:boolean; }
interface DeviceInfo { id:number; name:string; category?:string|null; }
interface ActiveDay { date:string; count:number; }

const App: React.FC = () => {
  const [deviceId, setDeviceId] = React.useState<string>('1');
  const [devices, setDevices] = React.useState<DeviceInfo[]>([]);
  const [devicesLoading, setDevicesLoading] = React.useState<boolean>(true);
  const [deviceMeta, setDeviceMeta] = React.useState<DeviceInfo|undefined>();
  const [from, setFrom] = React.useState<string>(''); // hidden selection (set via calendar)
  const [to, setTo] = React.useState<string>('');
  const [raw, setRaw] = React.useState<RawPoint[]>([]);
  const [cleaned, setCleaned] = React.useState<CleanedPoint[]>([]);
  const [showRaw, setShowRaw] = React.useState(true);
  const [showCleaned, setShowCleaned] = React.useState(true);
  const [year, setYear] = React.useState<number>(new Date().getUTCFullYear());
  const [monthStart, setMonthStart] = React.useState<number>(new Date().getUTCMonth()+1); // first of trio
  const [activeDays, setActiveDays] = React.useState<Record<string,ActiveDay>>({}); // key yyyy-mm-dd
  const [userCenter, setUserCenter] = React.useState<[number,number]|null>(null);
  const [stats, setStats] = React.useState<{outliers:number; corrected:number}>({outliers:0, corrected:0});
  const [showAlgo, setShowAlgo] = React.useState<boolean>(false);
  // ML param placeholder state (frontend only for now)
  const [mlParams, setMlParams] = React.useState({
    windowSize: 5,
    hampelSigma: 3.0,
    speedLimit: 120,
    maxAccel: 10,
    maxBearingChange: 40,
    crossTrackLimit: 80,
    useHampelOnSpeed: true,
    maxJumpMeters: 300,
    modelType: 'hampel',
    threshold: 0.5
  });
  const updateMl = (k: keyof typeof mlParams, v: any) => setMlParams(p => ({ ...p, [k]: v }));
  // Automatski čuvaj parametre u localStorage pri svakoj promeni
  React.useEffect(() => {
    localStorage.setItem('mlParams', JSON.stringify(mlParams));
  }, [mlParams]);

  // Pokušaj geolokacije korisnika (jednokratno) – ako uspe, mapu centriramo na njega dok nema podataka
  React.useEffect(()=> {
    if (!('geolocation' in navigator)) return;
    navigator.geolocation.getCurrentPosition(
      pos => setUserCenter([pos.coords.latitude, pos.coords.longitude]),
      () => setUserCenter([39,-98]) // fallback: približni centar SAD
    );
  }, []);

  // load device list once
  React.useEffect(()=> {
    (async()=>{
      try { const res = await axios.get('/api/devices'); setDevices(res.data); }
      catch { /* fallback demo */ if(devices.length===0) setDevices([{id:1,name:'Demo Device'}]); }
      finally { setDevicesLoading(false); }
    })();
  }, []);

  // update device meta when deviceId changes
  React.useEffect(()=> {
    const idNum = Number(deviceId);
    setDeviceMeta(devices.find(d=>d.id===idNum));
  }, [deviceId, devices]);

  // Fetch min/max when device changes
  React.useEffect(()=> {
    let cancelled = false;
    const fetchRange = async () => {
      if (!deviceId) return;
      try {
        const res = await axios.get(`/api/positions/device/${deviceId}/range`);
        if (cancelled) return;
        if (res.data.min && res.data.max) {
            const toIsoMinute = (date:Date) => date.toISOString().substring(0,16);
            // default window: min .. min + 1 day (or max if earlier)
            const minDate = new Date(res.data.min);
            const maxDate = new Date(res.data.max);
            const toDefault = new Date(minDate.getTime() + 24*3600*1000);
            const toChosen = toDefault > maxDate ? maxDate : toDefault;
            setFrom(toIsoMinute(minDate));
            setTo(toIsoMinute(toChosen));
            setYear(minDate.getUTCFullYear());
            setMonthStart(minDate.getUTCMonth()+1);
        }
      } catch (e) { /* ignore */ }
    };
    fetchRange();
    return ()=> { cancelled = true; };
  }, [deviceId]);

  // fetch active days when year or device changes (fetch whole year once stored)
  React.useEffect(()=> {
    let cancelled = false;
    (async()=>{
      if(!deviceId || !year) return;
      try {
        const res = await axios.get(`/api/positions/device/${deviceId}/activedays`, { params: { year }});
        if (cancelled) return;
        const map: Record<string,ActiveDay> = {};
        (res.data as any[]).forEach(d=> { map[d.date.substring(0,10)] = d; });
        setActiveDays(map);
      } catch(_){ }
    })();
    return ()=> { cancelled = true; };
  }, [deviceId, year]);

  const shiftMonths = (delta:number) => {
    setMonthStart(m => {
      let newMonth = m + delta;
      while(newMonth < 1) newMonth += 12;
      while(newMonth > 12) newMonth -= 12;
      return newMonth;
    });
  };

  const renderMonth = (m:number) => {
    const monthDate = new Date(Date.UTC(year, m-1, 1));
    const monthName = monthDate.toLocaleString('en-US', { month:'short'});
    const daysInMonth = new Date(Date.UTC(year, m, 0)).getUTCDate();
    const firstWeekDay = monthDate.getUTCDay(); // 0=Sun
    const cells: JSX.Element[] = [];
    for(let i=0;i<firstWeekDay;i++) cells.push(<div key={'e'+i} />);
    const selectedDate = (from && to && from.substring(0,10) === to.substring(0,10)) ? from.substring(0,10) : (from? from.substring(0,10): null);
    for(let d=1; d<=daysInMonth; d++) {
      const key = `${year}-${String(m).padStart(2,'0')}-${String(d).padStart(2,'0')}`;
      const active = !!activeDays[key];
      const selected = selectedDate === key;
      cells.push(
        <div
          key={key}
          style={{
            width:26,height:26,display:'flex',alignItems:'center',justifyContent:'center',
            margin:2,borderRadius:'50%',
            background: selected? '#000' : (active? 'orange':'#eee'),
            cursor: active? 'pointer':'default',
            fontSize:12,
            textDecoration: active? 'underline':'none',
            transition:'background 0.2s',
            color: selected? '#fff': undefined,
            ...(active ? { boxShadow: selected? '0 0 3px #000' : '0 0 2px #ff0' } : {}),
          }}
          title={active? `${activeDays[key].count} points`:'No data'}
          onClick={()=> {
            if(!active) return;
            const fromD = new Date(Date.UTC(year,m-1,d,0,0,0));
            const toDExclusive = new Date(Date.UTC(year,m-1,d+1,0,0,0)); // exclusive end
            const toIsoMinute = (date:Date)=> date.toISOString().substring(0,16);
            setFrom(toIsoMinute(fromD));
            setTo(toIsoMinute(new Date(toDExclusive.getTime()-60*1000))); // end minute (23:59)
            setTimeout(()=> load(), 0);
          }}
          onMouseOver={e=> { if(active && !selected) e.currentTarget.style.background='#ffd700'; }}
          onMouseOut={e=> { if(active && !selected) e.currentTarget.style.background='orange'; }}
        >
          {d}
        </div>
      );
    }
    return (
      <div key={'m'+m} style={{minWidth:200}}>
        <div style={{textAlign:'center', fontWeight:600}}>{monthName} {year}</div>
        <div style={{display:'grid', gridTemplateColumns:'repeat(7, 1fr)', fontSize:10, gap:0}}>
          {['Su','Mo','Tu','We','Th','Fr','Sa'].map(d=> <div key={d} style={{textAlign:'center',opacity:0.6}}>{d}</div>)}
          {cells}
        </div>
      </div>
    );
  };

  const load = async () => {
    const params: any = {
      from,
      to,
      windowSize: mlParams.windowSize,
      hampelSigma: mlParams.hampelSigma,
      speedLimit: mlParams.speedLimit,
      maxAccel: mlParams.maxAccel,
      maxBearingChange: mlParams.maxBearingChange,
      crossTrackLimit: mlParams.crossTrackLimit,
      useHampelOnSpeed: mlParams.useHampelOnSpeed,
      maxJumpMeters: mlParams.maxJumpMeters,
    };
    // Debug
    // eslint-disable-next-line no-console
    console.debug('Loading compare', params);
    const res = await axios.get(`/api/positions/device/${deviceId}/compare`, { params });
    const rawMapped = (res.data.raw||[]).map((r:any)=>({
      id:r.id ?? r.Id,
      deviceId:r.deviceId ?? r.DeviceId,
      deviceTime:r.deviceTime ?? r.DeviceTime,
      lat: Number(r.lat ?? r.Lat ?? r.latitude ?? r.Latitude),
      lng: Number(r.lng ?? r.Lng ?? r.longitude ?? r.Longitude),
      speedKph: Number(r.speedKph ?? r.SpeedKph ?? r.speed ?? r.Speed) || 0
    })).filter((p:any)=> Number.isFinite(p.lat) && Number.isFinite(p.lng));
    const cleanedMapped = (res.data.cleaned||[]).map((c:any)=>({
      id:c.id ?? c.Id,
      deviceId:c.deviceId ?? c.DeviceId,
      deviceTime:c.deviceTime ?? c.DeviceTime,
      lat: Number(c.lat ?? c.Lat),
      lng: Number(c.lng ?? c.Lng),
      speedKph: Number(c.speedKph ?? c.SpeedKph ?? 0),
      isOutlier: !!(c.isOutlier ?? c.IsOutlier),
      isInterpolated: !!(c.isInterpolated ?? c.IsInterpolated)
    })).filter((p:any)=> Number.isFinite(p.lat) && Number.isFinite(p.lng));
  setRaw(rawMapped as RawPoint[]);
  setCleaned(cleanedMapped as CleanedPoint[]);
  const outliers = cleanedMapped.filter((c:any)=> c.isOutlier).length;
  const corrected = cleanedMapped.filter((c:any)=> c.isInterpolated).length;
  setStats({ outliers, corrected });
    // eslint-disable-next-line no-console
    console.debug('Loaded counts raw/cleaned', rawMapped.length, cleanedMapped.length);
  };
  const rawLatLngs = raw.map((p:RawPoint)=> [p.lat,p.lng]) as [number,number][];
  const cleanedLatLngs = cleaned.map((p:CleanedPoint)=> [p.lat,p.lng]) as [number,number][];
  // Prioritet centriranja:
  // 1. Ako imamo podatke (cleaned ili raw) uzmi srednju tačku
  // 2. Ako nemamo podatke ali imamo dozvolu za lokaciju – korisnik
  // 3. Fallback: SAD (39,-98)
  const track = (cleanedLatLngs.length? cleanedLatLngs : rawLatLngs);
  const center = track.length > 0
    ? track[Math.floor(track.length/2)]
    : (userCenter || [39,-98]);

  React.useEffect(()=> {
    if (from && to && raw.length===0) {
      load();
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [from, to]);

  return (
  <div style={{padding:'0.5rem', height:'100%', boxSizing:'border-box', display:'flex', flexDirection:'column'}}>
      <h3 style={{marginBottom:'0.4rem'}}>ML GPS Cleaner (demo)</h3>
      {/* Top control bar */}
      <div style={{display:'flex', flexWrap:'wrap', gap:'0.75rem', alignItems:'center', padding:'6px 10px', background:'#f8f9fb', border:'1px solid #e1e5ea', borderRadius:6, marginBottom:'0.6rem'}}>
        <div style={{display:'flex', alignItems:'center', gap:6}}>
          <label style={{display:'flex', alignItems:'center', gap:4}}>Device:
            <select value={deviceId} onChange={(e:React.ChangeEvent<HTMLSelectElement>)=>setDeviceId(e.target.value)}>
              {devicesLoading && <option value="" disabled>Loading...</option>}
              {!devicesLoading && devices.length===0 && <option value="" disabled>No devices</option>}
              {devices.map(d=> <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </label>
          <span style={{fontSize:12, fontWeight:600, background:'#222', color:'#fff', padding:'3px 8px', borderRadius:4, letterSpacing:0.3}}>real-time cleaning path</span>
          {deviceMeta && <div style={{fontSize:12, padding:'3px 8px', background:'#eef1f5', borderRadius:4}}>{deviceMeta.category? `[${deviceMeta.category}] `:''}{deviceMeta.name}</div>}
        </div>
        <div style={{display:'flex', alignItems:'center', gap:6}}>
          <select value={year} onChange={(e:React.ChangeEvent<HTMLSelectElement>)=> setYear(Number(e.target.value))}>
            {(() => {
              const list: JSX.Element[] = [];
              const current = new Date().getUTCFullYear();
              for (let y = current; y >= 2020; y--) list.push(<option key={y} value={y}>{y}</option>);
              return list;
            })()}
          </select>
          <button onClick={()=> shiftMonths(-1)}>{'<'}</button>
          <button onClick={()=> shiftMonths(1)}>{'>'}</button>
          <button onClick={load}>Load</button>
        </div>
        <div style={{display:'flex', alignItems:'center', gap:10, flexWrap:'wrap'}}>
          <label style={{display:'flex',alignItems:'center',gap:4,fontSize:13}}><input type='checkbox' checked={showRaw} onChange={(e:React.ChangeEvent<HTMLInputElement>)=>setShowRaw(e.target.checked)} /> Raw</label>
          <label style={{display:'flex',alignItems:'center',gap:4,fontSize:13}}><input type='checkbox' checked={showCleaned} onChange={(e:React.ChangeEvent<HTMLInputElement>)=>setShowCleaned(e.target.checked)} /> Cleaned</label>
          <div style={{fontSize:12, opacity:0.85}}>
            <span style={{color:'red'}}>Raw outliers</span>, <span style={{color:'orange'}}>Raw normal</span>, <span style={{color:'green'}}>Cleaned route</span>
          </div>
          <div style={{display:'flex', alignItems:'center', gap:6}}>
            <div style={{fontSize:12, background:'#fff', border:'1px solid #ccc', padding:'2px 6px', borderRadius:4}}>
              Outliers: <strong>{stats.outliers}</strong> | Corrected: <strong>{stats.corrected}</strong>
            </div>
            {(from && to) && (
              <div style={{display:'flex', gap:4}}>
                <button style={{fontSize:11, background:'#e6ffe6', border:'1px solid #5ab65a'}} title='Accept current cleaned route (demo: no server call)'>Accept</button>
                <button style={{fontSize:11, background:'#ffecec', border:'1px solid #d66'}} title='Reject cleaned route (hide cleaned layer)' onClick={()=> { setShowCleaned(false); }}>Reject</button>
              </div>
            )}
          </div>
          <button style={{fontSize:11}} onClick={()=> setShowAlgo(s=>!s)}>{showAlgo? 'Hide rules':'Show rules'}</button>
        </div>
      </div>
      {showAlgo && (
        <div style={{marginBottom:'0.5rem', fontSize:12, background:'#fffff5', border:'1px solid #e0d9a8', padding:'6px 10px', borderRadius:4}}>
          <strong>Rule-based algorithms:</strong> speed threshold, acceleration threshold, bearing change per second, cross-track distance, Hampel filter (speed), interpolation of isolated outliers.
        </div>
      )}
  {/* Calendar + ML panel aligned in one responsive row */}
  <div style={{display:'flex', flexWrap:'wrap', alignItems:'flex-start', gap:'1.25rem', marginBottom:'0.75rem'}}>
        <div style={{display:'flex', gap:'1rem', flexWrap:'wrap', flex:'0 0 auto', minWidth:300}}>
          { [0,1,2].map(offset => renderMonth(((monthStart -1 + offset) % 12)+1)) }
        </div>
        {/* Parametri za čišćenje rute */}
        <div style={{flex:'1 1 320px', background:'#f3f7fa', border:'1px solid #b5c6e3', borderRadius:6, padding:'12px 14px', margin:'0 12px', boxShadow:'0 1px 2px rgba(0,0,0,0.04)'}}>
          <div style={{fontWeight:600, marginBottom:10}}>Route Cleaning Parameters</div>
          <div style={{display:'grid', gridTemplateColumns:'repeat(auto-fit,minmax(140px,1fr))', gap:'8px 12px', marginBottom:10}}>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Max speed (kph)
              <input type='number' min={1} max={400} value={mlParams.speedLimit ?? 120} onChange={e=>updateMl('speedLimit', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Max accel (km/h/s)
              <input type='number' step='0.1' min={0} max={50} value={mlParams.maxAccel ?? 10} onChange={e=>updateMl('maxAccel', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Max bearing Δ°/s
              <input type='number' step='1' min={0} max={180} value={mlParams.maxBearingChange ?? 40} onChange={e=>updateMl('maxBearingChange', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Cross-track (m)
              <input type='number' min={0} max={500} value={mlParams.crossTrackLimit ?? 80} onChange={e=>updateMl('crossTrackLimit', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Hampel window
              <input type='number' min={1} max={50} value={mlParams.windowSize ?? 5} onChange={e=>updateMl('windowSize', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Hampel σ
              <input type='number' step='0.1' min={0.1} max={10} value={mlParams.hampelSigma ?? 3.0} onChange={e=>updateMl('hampelSigma', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Use Hampel on speed
              <input type='checkbox' checked={mlParams.useHampelOnSpeed ?? true} onChange={e=>updateMl('useHampelOnSpeed', e.target.checked)} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Max jump (m)
              <input type='number' min={0} max={1000} value={mlParams.maxJumpMeters ?? 300} onChange={e=>updateMl('maxJumpMeters', Number(e.target.value))} />
            </label>
          </div>
          <div style={{display:'flex', gap:12, marginTop:8}}>
            <button type="button" style={{padding:'6px 18px', background:'#e0e7ef', border:'1px solid #b5c6e3', borderRadius:5, fontWeight:600, cursor:'pointer'}} onClick={()=>setMlParams({
              windowSize: 5,
              hampelSigma: 3.0,
              speedLimit: 120,
              maxAccel: 10,
              maxBearingChange: 40,
              crossTrackLimit: 80,
              useHampelOnSpeed: true,
              maxJumpMeters: 300,
              modelType: 'hampel',
              threshold: 0.5
            })}>Reset to defaults</button>
            <button type="button" style={{padding:'6px 18px', background:'#d6f0ff', border:'1px solid #7ec6f7', borderRadius:5, fontWeight:600, cursor:'pointer'}} onClick={load}>Refresh</button>
          </div>
        </div>
        <div style={{flex:'1 1 400px', background:'#f7f9ff', border:'1px solid #b5c6e3', borderRadius:6, padding:'12px 14px', boxShadow:'0 1px 2px rgba(0,0,0,0.05)', minWidth:340}}>
          <div style={{fontWeight:600, marginBottom:10}}>Machine Learning</div>
          <div style={{display:'grid', gridTemplateColumns:'repeat(auto-fit,minmax(130px,1fr))', gap:'8px 12px', marginBottom:10}}>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Window size
              <input type='number' min={1} max={200} value={mlParams.windowSize} onChange={e=>updateMl('windowSize', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Hampel σ
              <input type='number' step='0.1' min={0.1} max={10} value={mlParams.hampelSigma} onChange={e=>updateMl('hampelSigma', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Speed limit (kph)
              <input type='number' min={1} max={400} value={mlParams.speedLimit} onChange={e=>updateMl('speedLimit', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Max accel (m/s²)
              <input type='number' step='0.1' min={0} max={30} value={mlParams.maxAccel} onChange={e=>updateMl('maxAccel', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Max bearing Δ°/s
              <input type='number' step='1' min={0} max={180} value={mlParams.maxBearingChange} onChange={e=>updateMl('maxBearingChange', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Cross-track (m)
              <input type='number' min={1} max={5000} value={mlParams.crossTrackLimit} onChange={e=>updateMl('crossTrackLimit', Number(e.target.value))} />
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Model
              <select value={mlParams.modelType} onChange={e=>updateMl('modelType', e.target.value)}>
                <option value='PCA'>PCA</option>
                <option value='IsolationForest'>IsolationForest</option>
              </select>
            </label>
            <label style={{display:'flex', flexDirection:'column', fontSize:11, gap:2}}>Threshold
              <input type='range' min={0} max={1} step={0.01} value={mlParams.threshold} onChange={e=>updateMl('threshold', Number(e.target.value))} />
              <span style={{fontSize:10, textAlign:'center'}}>{mlParams.threshold.toFixed(2)}</span>
            </label>
          </div>
          <div style={{display:'flex', gap:8, flexWrap:'wrap', fontSize:11}}>
            <input type='file' style={{flex:'1 1 180px'}} title='Upload feature data (future)' disabled />
            <button disabled style={{padding:'4px 8px'}}>upload</button>
            <button disabled style={{padding:'4px 8px'}}>Database</button>
            <button disabled style={{padding:'4px 8px'}}>train</button>
            <button disabled style={{padding:'4px 8px'}}>load model</button>
            <button disabled style={{padding:'4px 8px'}}>score</button>
          </div>
        </div>
      </div>
  {/* Removed separate date badge; selection indicated directly on calendar (black circle). */}
  <MapContainer className='map' center={center as any} zoom={13} scrollWheelZoom style={{flex:'1 1 auto', width:'100%', border:'1px solid #ccc', minHeight:400}}>
        <TileLayer url='https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png' />
        <FitBounds raw={rawLatLngs} cleaned={cleanedLatLngs} />
        {showRaw && rawLatLngs.length>1 && <Polyline positions={rawLatLngs} pathOptions={{color:'orange', weight:2}} />}
        {showRaw && cleaned.length>0 && raw.map((p:RawPoint)=> {
            const cl = cleaned.find((c:CleanedPoint)=>c.id===p.id);
            const isOutlier = cl ? cl.isOutlier : false; // mapping by id
            return <CircleMarker key={'r'+p.id} center={[p.lat,p.lng]} radius={3} pathOptions={{color: isOutlier? 'red':'orange'}} />;
        })}
        {showCleaned && cleanedLatLngs.length>1 && <Polyline positions={cleanedLatLngs} pathOptions={{color:'green', weight:3}} />}
        {showCleaned && cleaned.map((c:CleanedPoint)=> c.isInterpolated && <CircleMarker key={'i'+c.id} center={[c.lat,c.lng]} radius={3} pathOptions={{color:'blue'}} />)}
      </MapContainer>
    </div>
  );
};

createRoot(document.getElementById('root')!).render(<App/>);

// Komponenta koja automatski prilagođava mapu kada se učita nova ruta
const FitBounds: React.FC<{raw:[number,number][], cleaned:[number,number][]}> = ({raw, cleaned}) => {
  const map = useMap();
  React.useEffect(()=> {
    const pts = (cleaned.length>1 ? cleaned : raw);
    if (pts.length>1) {
      const latlngs = pts.map(p=> ({lat:p[0], lng:p[1]}));
      // izračunaj bounds
      let minLat=latlngs[0].lat,maxLat=latlngs[0].lat,minLng=latlngs[0].lng,maxLng=latlngs[0].lng;
      for(const p of latlngs){
        if(p.lat<minLat)minLat=p.lat; if(p.lat>maxLat)maxLat=p.lat; if(p.lng<minLng)minLng=p.lng; if(p.lng>maxLng)maxLng=p.lng;
      }
      map.fitBounds([[minLat,minLng],[maxLat,maxLng]] as any, {padding:[20,20]});
    }
  }, [raw, cleaned, map]);
  return null;
};
