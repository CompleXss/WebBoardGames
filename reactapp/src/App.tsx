import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import Home from './components/Home/home';
import Login from './components/Login/login';
import NavPanel from './components/NavPanel/navPanel';
import History from './components/History/history';
import Leaderboard from './components/LeaderBoard/leaderboard';
import About from './components/About/about';
import Footer from './components/Footer/footer';
import CheckersLobby from './components/Games/Checkers/checkersLobby';
import CheckersGame from './components/Games/Checkers/checkersGame';
import './App.css'

export default function App() {
  return (
    <div className='wrapper'>
      <NavPanel />

      <main>
        <Routes>
          <Route path='/' element={<Home />} />
          <Route path='/login' element={<Login />} />
          <Route path='/history' element={<History />} />
          <Route path='/leaderboard' element={<Leaderboard />} />
          <Route path='/about' element={<About />} />
          <Route path='/lobby/checkers' element={<CheckersLobby />} />
          <Route path='/play/checkers' element={<CheckersGame />} />
          <Route path='*' element={<Navigate to={'/'} replace={true} />} />
        </Routes>
      </main>

      <Footer />
    </div>
  );
}
