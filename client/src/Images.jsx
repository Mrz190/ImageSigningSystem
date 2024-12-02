import React, { useState, useEffect } from 'react';
import CryptoJS from 'crypto-js';
import config from './config';

const Images = () => {
  const [images, setImages] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [modalImage, setModalImage] = useState(null);
  const role = sessionStorage.getItem('role');

  const getDigestAuthorizationHeader = async (method, uri) => {
    const local_HA1 = sessionStorage.getItem('userPasswordHash');
    const local_realm = sessionStorage.getItem('realm');
    const local_username = sessionStorage.getItem('username');

    if (!role || !local_HA1) {
      throw new Error('User is not authenticated or role is missing');
    }

    const nonceResponse = await fetch(`${config.apiBaseUrl}/Account/LoginNonce`, { method: 'GET' });

    if (!nonceResponse.ok) {
      throw new Error('Failed to fetch nonce');
    }

    const nonceData = await nonceResponse.json();
    const nonce = nonceData.nonce;

    const qop = 'auth';
    const nc = '00000001';
    const cnonce = CryptoJS.lib.WordArray.random(4).toString(CryptoJS.enc.Hex);

    const HA2 = CryptoJS.MD5(method + ":" + uri).toString(CryptoJS.enc.Hex);
    const response = CryptoJS.MD5(local_HA1 + ":" + nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + HA2).toString(CryptoJS.enc.Hex);

    return `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${response}"`;
  };

  const getUriForRole = (role) => {
    switch (role) {
      case 'User':
        return '/User/get-user-images';
      case 'Support':
        return '/Support/get-support-images';
      case 'Admin':
        return '/Admin/get-admin-images';
      default:
        throw new Error('Invalid role');
    }
  };

  const fetchImages = async () => {
    if (loading) return;
    setLoading(true);

    try {
      const uri = getUriForRole(role);
      const authorizationHeader = await getDigestAuthorizationHeader('GET', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'GET',
        headers: {
          'Authorization': authorizationHeader,
        },
      });

      if (!response.ok) {
        if (response.status === 404) {
          setImages([]);
        }
        return (
          <div>
            <button className="reload-btn" onClick={() => fetchImages()}></button>
            <div className="no-images">No images found</div>
          </div>
        );
      }

      const data = await response.json();
      setImages(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchImages();
  }, []);

  const handleDownloadImage = async (id) => {
    try {
      setLoading(true);

      const uri = `/User/download/${id}`;
      const authorizationHeader = await getDigestAuthorizationHeader('GET', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'GET',
        headers: {
          'Authorization': authorizationHeader,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to download image');
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `image-${id}.png`;
      link.click();
      window.URL.revokeObjectURL(url);

      fetchImages();
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteImage = async (id) => {
    if (!window.confirm('Are you sure you want to delete this image?')) return;

    try {
      setLoading(true);

      const uri = `/User/delete-image/${id}`;
      const authorizationHeader = await getDigestAuthorizationHeader('DELETE', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'DELETE',
        headers: {
          'Authorization': authorizationHeader,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to delete image');
      }

      setImages(images.filter((image) => image.id !== id));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleViewImage = async (id) => {
    try {
      setLoading(true);
      const uri = `/${role}/view-image/${id}`;
      const authorizationHeader = await getDigestAuthorizationHeader('GET', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'GET',
        headers: {
          'Authorization': authorizationHeader,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to fetch image');
      }

      const blob = await response.blob();
      const imageUrl = URL.createObjectURL(blob);
      setModalImage(imageUrl);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const closeModal = () => {
    setModalImage(null);
  };

  if (loading) {
    return (
      <div className="loading-container">
        <div className="spinner-container">
          <div className="spinner">
            <div></div>
            <div></div>
            <div></div>
            <div></div>
            <div></div>
            <div></div>
            <div></div>
            <div></div>
            <div></div>
            <div></div>
          </div>
          <p className="loading">Loading...</p>
        </div>
      </div>
    );
  }

  if (error) {
    console.log(error);
    return (
      <div>
        <button className="reload-btn" onClick={() => fetchImages()}></button>
        <div className="error">Error occurred while loading data. Try later.</div>
      </div>
    );
  }

  if (images.length === 0) {
    return (
      <div>
        <button className="reload-btn" onClick={() => fetchImages()}></button>
        <div className="no-images">No images found</div>
      </div>
    );
  }

  if (role === "Admin") {
    return (
      <div className="images-container">
        <button className="reload-btn" onClick={() => fetchImages()}></button>
        <table className="images-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Uploaded by</th>
              <th>Sign</th>
              <th>Reject</th>
              <th>View</th>
            </tr>
          </thead>
          <tbody>
            {images.map((image) => (
              <tr key={image.id}>
                <td>{image.imageName ? (image.imageName.length > 15 ? `${image.imageName.substring(0, 15)}...` : image.imageName) : "No Name"}</td>
                <td>{image.userName}</td>
                <td>
                  <button className="sign-btn" onClick={() => handleSignImage(image.id)}>Sign</button>
                </td>
                <td>
                  <button className="reject-btn" onClick={() => handleRejectImage(image.id)}>Reject</button>
                </td>
                <td>
                  <button className="view-btn" onClick={() => handleViewImage(image.id)}>View</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {modalImage && (
          <div className="modal">
            <div className="modal-content">
              <button className="close-btn" onClick={closeModal}></button>
              <img src={modalImage} alt="Viewed" className="modal-image" />
            </div>
          </div>
        )}
      </div>
    );
  } else if (role === "Support") {
    return (
      <div className="images-container">
        <button className="reload-btn" onClick={() => fetchImages()}></button>
        <table className="images-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Uploaded by</th>
              <th>Accept</th>
              <th>Reject</th>
              <th>View</th>
            </tr>
          </thead>
          <tbody>
            {images.map((image) => (
              <tr key={image.id}>
                <td>{image.imageName ? (image.imageName.length > 15 ? `${image.imageName.substring(0, 15)}...` : image.imageName) : "No Name"}</td>
                <td>{image.userName}</td>
                <td>
                  <button className="accept-btn" onClick={() => handleAcceptImage(image.id)}>Accept</button>
                </td>
                <td>
                  <button className="reject-btn" onClick={() => handleSupportRejectImage(image.id)}>Reject</button>
                </td>
                <td>
                  <button className="view-btn" onClick={() => handleViewImage(image.id)}>View</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {modalImage && (
          <div className="modal">
            <div className="modal-content">
              <button className="close-btn" onClick={closeModal}></button>
              <img src={modalImage} alt="Viewed" className="modal-image" />
            </div>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="images-container">
      <button className="reload-btn" onClick={() => fetchImages()}></button>
      <table className="images-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Status</th>
            <th>Download</th>
            <th>Delete</th>
            <th>View</th>
          </tr>
        </thead>
        <tbody>
          {images.map((image) => (
            <tr key={image.id}>
              <td>{image.imageName && image.imageName.length > 15 ? `${image.imageName.substring(0, 15)}...` : image.imageName || "No Name"}</td>
              <td>{image.status}</td>
              <td>
                <button className="download-btn" onClick={() => handleDownloadImage(image.id)}>Download</button>
              </td>
              <td>
                <button className="del-btn" onClick={() => handleDeleteImage(image.id)}>Delete</button>
              </td>
              <td>
                <button className="view-btn" onClick={() => handleViewImage(image.id)}>View</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {modalImage && (
        <div className="modal">
          <div className="modal-content">
            <button className="close-btn" onClick={closeModal}></button>
            <img src={modalImage} alt="Viewed" className="modal-image" />
          </div>
        </div>
      )}
    </div>
  );
};

export default Images;
