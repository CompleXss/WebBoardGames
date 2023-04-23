import React, { useState } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { NavPanel } from './components/navPanel';
import { Home } from './components/home';
import { History } from './components/history';
import { Leaderboard } from './components/leaderboard';
import { About } from './components/about';
import { Footer } from './components/footer';
import './App.css'

export default function App() {
  const [weather, setWeather] = useState([]);

  function getWeather() {
    const url = 'http://localhost:5041/weatherforecast'

    fetch(url, {
      method: 'GET',
    })
      .then(response => response.json())
      .then((weather) => {
        console.log(weather)
        setWeather(weather)
      })
      .catch((e) => {
        console.log(e)
        alert(e)
      })
  }

  return (
    <BrowserRouter>
      <div className='wrapper'>
        <NavPanel />

        <main>
          <Routes>
            <Route path='/' element={<Home />} />
            <Route path='/history' element={<History />} />
            <Route path='/leaderboard' element={<Leaderboard />} />
            <Route path='/about' element={<About />} />
            <Route path='*' element={<Navigate to={'/'} replace={true} />} />
          </Routes>
        </main>

        <Footer />
      </div>
    </BrowserRouter>
  );
}
