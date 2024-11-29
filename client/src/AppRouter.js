import React, { useEffect, useState } from "react";
import { BrowserRouter as Router, Routes, Route, useNavigate } from "react-router-dom";
import AuthForm from "./AuthForm";
import HomePage from "./HomePage";
import IndexPage from "./IndexPage";

function AppRouter() {
    const navigate = useNavigate();

    const [isAuthenticated, setIsAuthenticated] = useState(false); 

    useEffect(() => {
        const userPasswordHash = localStorage.getItem("userPasswordHash");
        if (userPasswordHash) {
            setIsAuthenticated(true);
        } 
    }, []);

    useEffect(() => {
        if (isAuthenticated) {
            navigate("/home");
        } else {
            navigate("/auth", { replace: true });
        }
    }, [isAuthenticated, navigate]); 

    return (
        <Routes>
            <Route path="/home" element={<HomePage />} />
            <Route path="/auth" element={<AuthForm />} />
            <Route path="/" element={<IndexPage />} />
        </Routes>
    );
}

export default AppRouter;
