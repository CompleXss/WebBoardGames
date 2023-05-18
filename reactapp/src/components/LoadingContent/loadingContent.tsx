import Loading from "../Loading/loading";
import './loadingContent.css'

export default function LoadingContent({ loading, content }: { loading: boolean, content: JSX.Element }) {
    return <div className='loadingContent'>
        {
            loading &&
            <div className='loadingBackground'><Loading /></div>
        }

        <div style={{ opacity: loading ? 0 : 1 }}>
            {content}
        </div>
    </div>
}