import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Home from './components/Home/home';
import NavPanel from './components/NavPanel/navPanel';
import History from './components/History/history';
import Leaderboard from './components/LeaderBoard/leaderboard';
import About from './components/About/about';
import Footer from './components/Footer/footer';
import './App.css'

export default function App() {
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
